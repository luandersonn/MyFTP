using FluentFTP;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

			Task.Run(async () => await RunAsync(default));
		}
		#endregion

		#region properties		
		public ReadOnlyObservableCollection<ITransferItem> TransferQueue { get; }
		#endregion

		#region methods		
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
							await AccessUI(() => _transferItems.Remove(item));
						});
						item.CancelRequested -= CanceledRequested;
					}
				});
			}
		}

		private async void CanceledRequested(object sender, EventArgs args)
		{
			await AccessUIAsync(async () =>
			{
				var item = (ITransferItem)sender;
				item.CancelRequested -= CanceledRequested;
				await Task.Delay(TimeSpan.FromSeconds(2));
				_transferItems.Remove(item);
				_tokens.Remove(item);
			});
		}

		private async Task AccessUI(Action function)
		{
			await _dispatcher.EnqueueAsync(function, DispatcherQueuePriority.Low);
		}
		private async Task AccessUIAsync(Func<Task> function)
		{
			await _dispatcher.EnqueueAsync(function, DispatcherQueuePriority.Low);
		}
		#endregion
	}
}
