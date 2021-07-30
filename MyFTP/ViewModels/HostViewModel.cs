using FluentFTP;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.Collections;
using MyFTP.Services;
using MyFTP.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Utils.Comparers;

namespace MyFTP.ViewModels
{
	public class HostViewModel : BindableItem
	{
		private IObservableSortedCollection<FtpListItemViewModel> _items;
		public IFtpClient Client { get; private set; }
		public ITransferItemService TransferService { get; }
		public IDialogService DialogService { get; }
		public ReadOnlyObservableCollection<FtpListItemViewModel> Root { get; }

		public IAsyncRelayCommand<FtpListItemViewModel> RefreshCommand { get; }
		public IAsyncRelayCommand<FtpListItemViewModel> UploadCommand { get; }
		public IAsyncRelayCommand<IEnumerable<FtpListItemViewModel>> DownloadCommand { get; }
		public IAsyncRelayCommand<FtpListItemViewModel> DeleteCommand { get; }

		public HostViewModel(IFtpClient client, ITransferItemService transferService, IDialogService dialogService)
		{
			Client = client;
			TransferService = transferService;
			DialogService = dialogService;
			_items = new ObservableSortedCollection<FtpListItemViewModel>(new FtpListItemComparer());
			Root = new ReadOnlyObservableCollection<FtpListItemViewModel>((ObservableSortedCollection<FtpListItemViewModel>)_items);

			RefreshCommand = new AsyncRelayCommand<FtpListItemViewModel>
				(async item => await item.RefreshCommand.ExecuteAsync(null),
				item => IsNotNull(item) && item.RefreshCommand.CanExecute(null));

			UploadCommand = new AsyncRelayCommand<FtpListItemViewModel>
				(async item => await item.UploadCommand.ExecuteAsync(null),
				item => IsNotNull(item) && item.UploadCommand.CanExecute(null));

			DownloadCommand = new AsyncRelayCommand<IEnumerable<FtpListItemViewModel>>(DownloadCommandAsync, CanExecuteDownloadCommand);
			
			DeleteCommand = new AsyncRelayCommand<FtpListItemViewModel>(
				async (item) => await item.DeleteCommand.ExecuteAsync(null),
				(item) => item.DeleteCommand.CanExecute(item));
		}

		private async Task DownloadCommandAsync(IEnumerable<FtpListItemViewModel> items)
		{
			var folder = await WeakReferenceMessenger.Default.Send<RequestOpenFolderMessage>();
			if (folder != null)
			{
				foreach (var item in items)
				{
					var file = await folder.CreateFileAsync(item.Name, Windows.Storage.CreationCollisionOption.GenerateUniqueName);
					TransferService.EnqueueDownload(Client, item.FullName, file);
				}
			}
		}

		#region can execute
		private bool CanExecuteDownloadCommand(IEnumerable<FtpListItemViewModel> items)
		{
			return items != null && items.Any() && items.All(item => item.DownloadCommand.CanExecute(null));
		}
		#endregion

		public async Task LoadRootAsync()
		{
			if (!_items.Any())
			{
				var item = await Client.GetObjectInfoAsync("/");
				_items.AddItem(new FtpListItemViewModel(Client, item, null, TransferService, DialogService));
			}
		}

		public async Task DisconnectAsync() => await Client.DisconnectAsync();

		private bool IsNotNull(FtpListItemViewModel item) => item != null;
	}
}