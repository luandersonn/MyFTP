using FluentFTP;
using System.Collections.ObjectModel;
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
	}
}