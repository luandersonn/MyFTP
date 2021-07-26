using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyFTP.Services
{
	public interface ITransferItem : INotifyPropertyChanged
	{
		IStorageItem StorageItem { get; }
		string RemotePath { get; }
		TransferItemType Type { get; }
		TransferItemStatus Status { get; }
		double Progress { get; }
		Task StartAsync(CancellationToken token);
		void Cancel();
		Exception Exception { get; }
		event EventHandler<EventArgs> CancelRequested;
	}
}