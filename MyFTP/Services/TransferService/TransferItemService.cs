using FluentFTP;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace MyFTP.Services
{
	public class TransferItemService : ITransferItemService
	{
		#region fields		
		private readonly IDictionary<ITransferItem, string> _tokens;
		private readonly BlockingCollection<ITransferItem> _producer;
		private readonly ObservableCollection<ITransferItem> _transferItems;
		private readonly DispatcherQueue _dispatcher;
		private readonly WeakReferenceMessenger _weakMessenger;
		private CancellationTokenSource _source;
		private ITransferItem _currentItem;

		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region constructor		
		public TransferItemService()
		{
			_producer = new BlockingCollection<ITransferItem>();
			_transferItems = new ObservableCollection<ITransferItem>();
			_dispatcher = DispatcherQueue.GetForCurrentThread();
			_tokens = new Dictionary<ITransferItem, string>();
			_weakMessenger = WeakReferenceMessenger.Default;
			TransferQueue = new ReadOnlyObservableCollection<ITransferItem>(_transferItems);
		}
		#endregion

		#region properties		
		public ReadOnlyObservableCollection<ITransferItem> TransferQueue { get; }
		public ITransferItem CurrentItem
		{
			get => _currentItem;
			private set
			{
				if (!Equals(_currentItem, value))
				{
					_currentItem = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentItem)));
				}
			}
		}
		#endregion

		#region methods		
		public void Start()
		{
			_source?.Cancel();
			_source = new CancellationTokenSource();
			Task.Run(async () => await RunAsync(_source.Token));
		}
		public void Stop() => _source?.Cancel();
		public void EnqueueDownload(IFtpClient client, string remoteFilePath, IStorageFile destinationFile)
		{
			if (client is null) throw new ArgumentNullException(nameof(client));
			if (string.IsNullOrEmpty(remoteFilePath)) throw new ArgumentException($"'{nameof(remoteFilePath)}' não pode ser nulo nem vazio.", nameof(remoteFilePath));
			if (destinationFile is null) throw new ArgumentNullException(nameof(destinationFile));
			var item = new TransferItem(client, remoteFilePath, destinationFile, TransferItemType.Download);
			_producer.Add(item);
			_transferItems.Add(item);
			item.CancelRequested += CanceledRequested;
		}
		public void EnqueueDownload(IFtpClient client, string remoteFolderPath, IStorageFolder destinationFolder)
		{
			if (client is null) throw new ArgumentNullException(nameof(client));
			if (string.IsNullOrEmpty(remoteFolderPath)) throw new ArgumentException($"'{nameof(remoteFolderPath)}' não pode ser nulo nem vazio.", nameof(remoteFolderPath));
			if (destinationFolder is null) throw new ArgumentNullException(nameof(destinationFolder));
			var item = new TransferItem(client, remoteFolderPath, destinationFolder, TransferItemType.Download);
			_producer.Add(item);
			_transferItems.Add(item);
			item.CancelRequested += CanceledRequested;
		}
		public void EnqueueUpload(IFtpClient client, string remoteFilePath, IStorageFile localFile, string token)
		{
			if (client is null) throw new ArgumentNullException(nameof(client));
			if (string.IsNullOrEmpty(remoteFilePath)) throw new ArgumentException($"'{nameof(remoteFilePath)}' não pode ser nulo nem vazio.", nameof(remoteFilePath));
			if (localFile is null) throw new ArgumentNullException(nameof(localFile));
			var item = new TransferItem(client, remoteFilePath, localFile, TransferItemType.Upload);
			_producer.Add(item);
			_transferItems.Add(item);
			_tokens.Add(item, token);
			item.CancelRequested += CanceledRequested;
		}
		public void EnqueueUpload(IFtpClient client, string remoteFolderPath, IStorageFolder localFolder, string token)
		{
			if (client is null) throw new ArgumentNullException(nameof(client));
			if (string.IsNullOrEmpty(remoteFolderPath)) throw new ArgumentException($"'{nameof(remoteFolderPath)}' não pode ser nulo nem vazio.", nameof(remoteFolderPath));
			if (localFolder is null) throw new ArgumentNullException(nameof(localFolder));
			var item = new TransferItem(client, remoteFolderPath, localFolder, TransferItemType.Upload);
			_producer.Add(item);
			_transferItems.Add(item);
			_tokens.Add(item, token);
			item.CancelRequested += CanceledRequested;
		}
		public async Task DownloadAsync(IFtpClient client, string remoteFilePath, IStorageFile destinationFile, IProgress<double> progress, CancellationToken token)
		{
			await TransferAsync(client, remoteFilePath, destinationFile, TransferItemType.Download, progress, token);
		}
		public async Task DownloadAsync(IFtpClient client, string remoteFolderPath, IStorageFolder destinationFolder, IProgress<double> progress, CancellationToken token)
		{
			await TransferAsync(client, remoteFolderPath, destinationFolder, TransferItemType.Download, progress, token);
		}
		public async Task UploadAsync(IFtpClient client, string remoteFilePath, IStorageFile destinationFile, IProgress<double> progress, CancellationToken token)
		{
			await TransferAsync(client, remoteFilePath, destinationFile, TransferItemType.Upload, progress, token);
		}
		public async Task UploadAsync(IFtpClient client, string remoteFolderPath, IStorageFolder destinationFolder, IProgress<double> progress, CancellationToken token)
		{
			await TransferAsync(client, remoteFolderPath, destinationFolder, TransferItemType.Upload, progress, token);
		}
		private async Task TransferAsync(IFtpClient client, string remotePath, IStorageItem storageItem, TransferItemType type, IProgress<double> progress, CancellationToken token)
		{
			var progressReport = new Progress<FtpProgress>(x => progress.Report(x.Progress / 100.0));
			var item = new TransferItem(client, remotePath, storageItem, type, progressReport);
			await item.StartAsync(token);
			if (item.Exception != null)
				throw item.Exception;
		}

		private async Task RunAsync(CancellationToken cancellationToken)
		{
			while (true)
			{
				// Get item from queue and start transfer
				var item = _producer.Take(cancellationToken);

				await AccessUIAsync(async () =>
				{
					try
					{
						CurrentItem = item;
						await item.StartAsync(cancellationToken);
						// Transfer completed, notify the sender about it
						if (item.Status == TransferItemStatus.Completed && _tokens.TryGetValue(item, out var token))
						{
							_weakMessenger.Send<object, string>(item, token);
							_tokens.Remove(item);
						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
					finally
					{
						// Create a Task to remove item from ObservableCollection after 10 seconds and go to next transfer item						
						_ = Task.Run(async () =>
						{
							// Wait 10 secs before remove from transfer list							
							await Task.Delay(TimeSpan.FromSeconds(10));
							await AccessUI(() =>
							{
								_transferItems.Remove(item);
								if (CurrentItem == item)
									CurrentItem = null;
							});
						});
						item.CancelRequested -= CanceledRequested;
					}
				});
			}
		}
		private async void CanceledRequested(object sender, EventArgs args)
		{
			await AccessUI(() =>
			{
				var item = (ITransferItem)sender;
				item.CancelRequested -= CanceledRequested;
				_transferItems.Remove(item);
				_tokens.Remove(item);
			});
		}
		private async Task AccessUI(Action function)
		{
			await _dispatcher.EnqueueAsync(function, DispatcherQueuePriority.Normal);
		}
		private async Task AccessUIAsync(Func<Task> function)
		{
			await _dispatcher.EnqueueAsync(function, DispatcherQueuePriority.Normal);
		}
		#endregion
	}
}
