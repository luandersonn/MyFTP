using FluentFTP;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyFTP.Services
{
	public interface ITransferItemService
	{
		ReadOnlyObservableCollection<ITransferItem> TransferQueue { get; }
		void EnqueueDownload(IFtpClient client, string remoteFilePath, IStorageFile destinationFile);
		void EnqueueDownload(IFtpClient client, string remoteFolderPath, IStorageFolder destinationFolder);
		void EnqueueUpload(IFtpClient client, string remoteFilePath, IStorageFile localFile, string token);
		void EnqueueUpload(IFtpClient client, string remoteFolderPath, IStorageFolder localFolder, string token);

		Task DownloadAsync(IFtpClient client, string remoteFilePath, IStorageFile destinationFile, IProgress<double> progress, CancellationToken token);
		Task DownloadAsync(IFtpClient client, string remoteFolderPath, IStorageFolder destinationFolder, IProgress<double> progress, CancellationToken token);
		Task UploadAsync(IFtpClient client, string remoteFilePath, IStorageFile destinationFile, IProgress<double> progress, CancellationToken token);
		Task UploadAsync(IFtpClient client, string remoteFolderPath, IStorageFolder destinationFolder, IProgress<double> progress, CancellationToken token);
	}
}