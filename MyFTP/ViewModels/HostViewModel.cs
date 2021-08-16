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
using System.Threading;
using System.Threading.Tasks;
using Utils.Comparers;
using Windows.Storage;

namespace MyFTP.ViewModels
{
	public class HostViewModel : BindableItem
	{
		private IObservableSortedCollection<FtpListItemViewModel> _root;
		public ITransferItemService TransferService { get; }
		public IDialogService DialogService { get; }
		public ReadOnlyObservableCollection<FtpListItemViewModel> Root { get; }
		public IAsyncRelayCommand<FtpListItemViewModel> RefreshCommand { get; }
		public IAsyncRelayCommand<FtpListItemViewModel> UploadCommand { get; }
		public IAsyncRelayCommand<IEnumerable<FtpListItemViewModel>> DownloadCommand { get; }
		public IAsyncRelayCommand<FtpListItemViewModel> DeleteCommand { get; }
		public IAsyncRelayCommand<FtpListItemViewModel> DisconnectCommand { get; }

		public HostViewModel(ITransferItemService transferService, IDialogService dialogService)
		{
			TransferService = transferService;
			transferService?.Start();
			DialogService = dialogService;
			_root = new ObservableSortedCollection<FtpListItemViewModel>(new FtpListItemComparer());
			Root = new ReadOnlyObservableCollection<FtpListItemViewModel>((ObservableSortedCollection<FtpListItemViewModel>)_root);

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

			DisconnectCommand = new AsyncRelayCommand<FtpListItemViewModel>(OnDisconnectCommandAsync, IsNotNull);
		}		

		private async Task DownloadCommandAsync(IEnumerable<FtpListItemViewModel> items)
		{
			var folder = await WeakReferenceMessenger.Default.Send<RequestOpenFolderMessage>();
			if (folder != null)
			{
				foreach (var item in items)
				{
					if (item.Type == FtpFileSystemObjectType.Directory)
					{
						var newFolder = await folder.CreateFolderAsync(item.Name, CreationCollisionOption.ReplaceExisting);
						TransferService.EnqueueDownload(item.Client, item.FullName, newFolder);
					}
					else
					{
						var file = await folder.CreateFileAsync(item.Name, CreationCollisionOption.GenerateUniqueName);
						TransferService.EnqueueDownload(item.Client, item.FullName, file);
					}
				}
			}
		}

		private async Task OnDisconnectCommandAsync(FtpListItemViewModel arg, CancellationToken token)
		{
			var root = arg;
			try
			{
				// Get the root
				while (root.Parent != null)
					root = root.Parent;
				await root.Client.DisconnectAsync(token);
			}
			finally
			{
				_root.RemoveItem(root);
			}
		}

		#region can execute
		private bool CanExecuteDownloadCommand(IEnumerable<FtpListItemViewModel> items)
		{
			return items != null && items.Any() && items.All(item => item.DownloadCommand.CanExecute(null));
		}
		#endregion

		public void AddItem(FtpListItemViewModel root)
		{
			_root.AddItem(root);
		}

		private bool IsNotNull(FtpListItemViewModel item) => item != null;
	}
}