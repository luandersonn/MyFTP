﻿using FluentFTP;
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
using Windows.Storage;

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
			transferService?.Start();
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
					if (item.Type == FtpFileSystemObjectType.Directory)
					{
						var newFolder = await folder.CreateFolderAsync(item.Name, CreationCollisionOption.ReplaceExisting);
						TransferService.EnqueueDownload(Client, item.FullName, newFolder);
					}
					else
					{
						var file = await folder.CreateFileAsync(item.Name, CreationCollisionOption.GenerateUniqueName);
						TransferService.EnqueueDownload(Client, item.FullName, file);
					}
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

				if (item is null)
				{
					var d = default(DateTime);
					item = new FtpListItem("", "/" , -1, true, ref d)
					{
						FullName = "/",
						Type = FtpFileSystemObjectType.Directory						
					};
				}
				_items.AddItem(new FtpListItemViewModel(Client, item, null, TransferService, DialogService));
			}
		}

		public async Task DisconnectAsync() => await Client.DisconnectAsync();

		private bool IsNotNull(FtpListItemViewModel item) => item != null;
	}
}