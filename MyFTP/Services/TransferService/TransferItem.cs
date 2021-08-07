using FluentFTP;
using MyFTP.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyFTP.Services
{
	public class TransferItem : BindableItem, ITransferItem
	{
		#region fields		
		private readonly IFtpClient _client;
		private readonly CancellationTokenSource _source;
		private TransferItemStatus _status;
		private double _progress;
		private IProgress<FtpProgress> progress;
		private Exception _exception;

		public event EventHandler<EventArgs> CancelRequested;
		#endregion

		#region constructor	
		public TransferItem(IFtpClient client, string remoteFilePath, IStorageItem destinationFile, TransferItemType type, IProgress<FtpProgress> progress = null)
		{
			_client = client ?? throw new ArgumentNullException(nameof(client));
			RemotePath = remoteFilePath ?? throw new ArgumentNullException(nameof(remoteFilePath));
			StorageItem = destinationFile ?? throw new ArgumentNullException(nameof(destinationFile));
			Type = type;

			Status = TransferItemStatus.Idle;
			Progress = 0;

			_source = new CancellationTokenSource();
			this.progress = progress ?? new Progress<FtpProgress>(ProgressUpdate);
		}
		#endregion

		#region properties		
		public IStorageItem StorageItem { get; set; }
		public string RemotePath { get; set; }
		public TransferItemType Type { get; set; }
		public TransferItemStatus Status { get => _status; private set => Set(ref _status, value); }
		public double Progress { get => _progress; private set => Set(ref _progress, value); }
		public Exception Exception { get => _exception; private set => Set(ref _exception, value); }
		#endregion

		#region methods
		public void Cancel() => _source.Cancel();

		public async Task StartAsync(CancellationToken token)
		{
			// When token is used to cancel the transfer, cancel using the _source
			token.Register(() => _source.Cancel());
			Status = TransferItemStatus.Running;
			try
			{

				switch (Type)
				{
					case TransferItemType.Download:
						{
							if (StorageItem.IsOfType(StorageItemTypes.File))
							{
								await DownloadFileAsync((IStorageFile)StorageItem, _source.Token);
							}
							else
							{
								await DownloadDirectoryAsync((IStorageFolder)StorageItem, _source.Token);
							}
							break;
						}
					case TransferItemType.Upload:
						{
							if (StorageItem.IsOfType(StorageItemTypes.File))
							{
								await UploadFileAsync((IStorageFile)StorageItem, _source.Token);
							}
							else
							{
								await UploadDirectoryAsync((IStorageFolder)StorageItem, _source.Token);
							}
							break;
						}
					default:
						throw new NotSupportedException();
				}
				Status = TransferItemStatus.Completed;
			}
			catch (OperationCanceledException e)
			{
				Status = TransferItemStatus.Canceled;
				Exception = e;
				CancelRequested?.Invoke(this, new EventArgs());
			}
			catch (Exception e)
			{
				Status = TransferItemStatus.Error;
				Exception = e;
				throw;
			}
		}

		private async Task UploadFileAsync(IStorageFile file, CancellationToken token)
		{
			// Copy file to Local storage due UWP limiations using System.IO
			var tempFolder = ApplicationData.Current.TemporaryFolder;
			// Random file name
			var tempFileName = Guid.NewGuid().ToString();
			// Se houver uma colisão de Guid, pelo amor de Deus né, mas se prepara para essa situação difícil
			var tempFile = await tempFolder.CreateFileAsync(tempFileName, CreationCollisionOption.GenerateUniqueName).AsTask(token);
			try
			{
				await file.CopyAndReplaceAsync(tempFile).AsTask(token);
				var result = await _client.UploadFileAsync(tempFile.Path, RemotePath, FtpRemoteExists.Overwrite, false, FtpVerify.None, progress, token);
				// Update Modified date
				var basicProps = await file.GetBasicPropertiesAsync();
				if (basicProps != null)
				{
					await _client.SetModifiedTimeAsync(RemotePath, basicProps.DateModified.Date);
				}

				switch (result)
				{
					case FtpStatus.Failed:
						throw new FtpException("Failed");
					case FtpStatus.Skipped:
						throw new FtpException("Upload skipped");
				}
			}
			catch (OperationCanceledException)
			{
				_client.DeleteFile(RemotePath);
				throw;
			}
			finally
			{
				await tempFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
		}

		private async Task UploadDirectoryAsync(IStorageFolder folder, CancellationToken token)
		{
			var tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(Guid.NewGuid().ToString(), CreationCollisionOption.GenerateUniqueName);
			try
			{
				await CopyFolderAsync(folder, tempFolder, token);
				await _client.UploadDirectoryAsync(tempFolder.Path, RemotePath, FtpFolderSyncMode.Update, FtpRemoteExists.Skip, FtpVerify.None, progress: progress, token: token);
			}
			finally
			{
				await tempFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
		}

		private async Task DownloadFileAsync(IStorageFile file, CancellationToken token)
		{
			// Copy file to Local storage due UWP limiations using System.IO
			var tempFolder = ApplicationData.Current.TemporaryFolder;
			// Random file name
			var tempFileName = Guid.NewGuid().ToString();
			// Se houver uma colisão de Guid, pelo amor de Deus né, mas se prepara para essa situação difícil
			var tempFile = await tempFolder.CreateFileAsync(tempFileName, CreationCollisionOption.GenerateUniqueName).AsTask(token);
			try
			{
				var result = await _client.DownloadFileAsync(tempFile.Path, RemotePath, FtpLocalExists.Overwrite, FtpVerify.None, progress, token);
				switch (result)
				{
					case FtpStatus.Failed:
						throw new FtpException("Failed");
					case FtpStatus.Skipped:
						throw new FtpException("Upload skipped");
				}
				await tempFile.CopyAndReplaceAsync(file);
			}
			finally
			{
				await tempFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
		}


		private async Task DownloadDirectoryAsync(IStorageFolder folder, CancellationToken token)
		{
			var tempFolder = ApplicationData.Current.TemporaryFolder;
			var tempFolderName = Guid.NewGuid().ToString();
			tempFolder = await tempFolder.CreateFolderAsync(tempFolderName, CreationCollisionOption.ReplaceExisting);
			try
			{
				var result = await _client.DownloadDirectoryAsync(tempFolder.Path,
																		RemotePath,
																		FtpFolderSyncMode.Update,
																		FtpLocalExists.Overwrite,
																		FtpVerify.None,
																		progress: progress,
																		token: token);
				await CopyFolderAsync(tempFolder, folder);
			}
			finally
			{
				await tempFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
		}

		// https://stackoverflow.com/a/27797685/4811833
		public async Task CopyFolderAsync(IStorageFolder source, IStorageFolder destinationFolder, CancellationToken token = default)
		{
			foreach (var file in await source.GetFilesAsync().AsTask(token))
			{
				await file.CopyAsync(destinationFolder, file.Name, NameCollisionOption.ReplaceExisting);
			}
			foreach (var folder in await source.GetFoldersAsync().AsTask(token))
			{
				var newFolder = await destinationFolder.CreateFolderAsync(folder.Name, CreationCollisionOption.ReplaceExisting).AsTask(token);
				await CopyFolderAsync(folder, newFolder, token);
			}
		}

		private void ProgressUpdate(FtpProgress progress)
		{
			Progress = progress.Progress / 100.0;
		}
		#endregion
	}
}