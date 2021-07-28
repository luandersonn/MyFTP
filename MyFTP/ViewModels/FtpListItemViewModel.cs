using FluentFTP;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.Collections;
using MyFTP.Services;
using MyFTP.Utils;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils.Comparers;
using Windows.System;

namespace MyFTP.ViewModels
{
	public class FtpListItemViewModel : BindableItem
	{
		#region fields		
		private readonly IObservableSortedCollection<FtpListItemViewModel> _items;
		private bool _isLoaded;
		private bool _isLoading;
		private readonly FtpListItem _ftpItem;
		private readonly IFtpClient _client;
		private readonly WeakReferenceMessenger _weakMessenger;
		private readonly ITransferItemService _transferService;
		private readonly IDialogService _dialogService;
		private readonly string _guid;
		#endregion

		#region constructor
		private FtpListItemViewModel(IFtpClient client, FtpListItemViewModel parent, ITransferItemService transferService, IDialogService dialogService)
		{
			_client = client ?? throw new ArgumentNullException(nameof(client));
			Parent = parent;

			_items = new ObservableSortedCollection<FtpListItemViewModel>(new FtpListItemComparer());
			Items = new ReadOnlyObservableCollection<FtpListItemViewModel>((ObservableCollection<FtpListItemViewModel>)_items);

			_isLoaded = _isLoading = false;

			RefreshCommand = new AsyncRelayCommand(RefreshCommandAsync, CanExecuteRefreshCommand);
			UploadCommand = new AsyncRelayCommand(UploadCommandAsync, CanExecuteUploadCommand);
			DownloadCommand = new AsyncRelayCommand(DownloadCommandAsync, CanExecuteDownloadCommand);
			DeleteCommand = new AsyncRelayCommand(DeleteCommandAsync, CanExecuteDeleteCommand);
			CreateFolderCommand = new AsyncRelayCommand<string>(CreateFolderCommandAsync, CanExecuteCreateFolderCommand);
			

			Dispatcher = DispatcherQueue.GetForCurrentThread();

			_weakMessenger = WeakReferenceMessenger.Default;

			_transferService = transferService;
			_dialogService = dialogService;
			_guid = Guid.NewGuid().ToString();
			_weakMessenger.Register<object, string>(this, _guid, UploadFinished);

			_client.EnableThreadSafeDataConnections = true;
		}

		public FtpListItemViewModel(IFtpClient client, string name, string fullName, ITransferItemService transferService, IDialogService dialogService)
			: this(client, null, transferService, dialogService)
		{
			Name = name;
			FullName = fullName;
			Type = FtpFileSystemObjectType.Directory;
		}

		public FtpListItemViewModel(IFtpClient client, FtpListItem item, FtpListItemViewModel parent, ITransferItemService transferService, IDialogService dialogService)
			: this(client, parent, transferService, dialogService)
		{
			_ftpItem = item ?? throw new ArgumentNullException(nameof(item));
			Name = item.Name;
			FullName = item.FullName;
			Type = item.Type;
			SubType = item.SubType;
			Size = item.Size;
			Modified = item.Modified;
			OwnerPermissions = item.OwnerPermissions;
		}
		#endregion

		#region properties		
		public string Name { get; }
		public string FullName { get; }
		public FtpPermission OwnerPermissions { get; }
		public FtpListItemViewModel Parent { get; }
		public FtpFileSystemObjectType Type { get; }
		public FtpFileSystemObjectSubType SubType { get; }
		public bool IsDirectory => Type == FtpFileSystemObjectType.Directory;
		public long Size { get; }
		public DateTime Modified { get; }
		public ReadOnlyObservableCollection<FtpListItemViewModel> Items { get; }
		public bool IsLoaded { get => _isLoaded; private set => Set(ref _isLoaded, value); }
		public bool IsLoading { get => _isLoading; private set => Set(ref _isLoading, value); }
		#endregion

		#region commands
		public IAsyncRelayCommand RefreshCommand { get; }
		public IAsyncRelayCommand UploadCommand { get; }
		public IAsyncRelayCommand DownloadCommand { get; }	
		public IAsyncRelayCommand DeleteCommand { get; }
		public IAsyncRelayCommand<string> CreateFolderCommand { get; }

		public async Task RefreshCommandAsync(CancellationToken token = default)
		{
			await AccessUIAsync(() => IsLoading = true);
			RefreshCommand.NotifyCanExecuteChanged();
			try
			{
				if (Type != FtpFileSystemObjectType.Directory)
					throw new NotSupportedException();
				var result = await _client.GetListingAsync(FullName, token);
				await AccessUIAsync(() =>
				{
					_items.Clear();
					foreach (var item in result)
					{
						_items.AddItem(new FtpListItemViewModel(_client, item, this, _transferService, _dialogService));
					}
					IsLoaded = true;
				});
			}
			catch
			{
				await AccessUIAsync(() => IsLoaded = false);
				throw;
			}
			finally
			{
				await AccessUIAsync(() => IsLoading = false);
				RefreshCommand.NotifyCanExecuteChanged();
			}
		}
		private async Task UploadCommandAsync(CancellationToken token)
		{
			var files = await _weakMessenger.Send<RequestOpenFilesMessage>();			
			if (files != null)
			{
				foreach (var file in files)
				{
					bool result = true;
					var remotePath = string.Format("{0}/{1}", FullName, file.Name);
					if (_dialogService != null && await _client.GetObjectInfoAsync(remotePath, token: token) is FtpListItem current)
					{
						result = await _dialogService.AskForReplaceAsync(file, new FtpListItemViewModel(_client, current, this, null, null));
					}
					if (result)
						_transferService.EnqueueUpload(_client, remotePath, file, _guid);
				}
			}
		}

		private async Task DownloadCommandAsync(CancellationToken token)
		{
			var file = await _weakMessenger.Send<RequestSaveFileMessage>(new RequestSaveFileMessage() { FileNameSuggestion = Name });
			if (file != null)
			{
				_transferService.EnqueueDownload(_client, FullName, file);
			}
		}

		private async Task DeleteCommandAsync(CancellationToken token)
		{
			if (await _dialogService.AskForDeleteAsync(this))
			{
				if (Type == FtpFileSystemObjectType.Directory)
					await _client.DeleteDirectoryAsync(FullName, token);
				else
					await _client.DeleteFileAsync(FullName, token);
				if (Parent != null)
					Parent._items.RemoveItem(this);
			}
		}

		private async Task CreateFolderCommandAsync(string folderName, CancellationToken token)
		{
			var remotePath = string.Format("{0}/{1}", FullName, folderName);
			if (await _client.CreateDirectoryAsync(remotePath, false, token))
			{
				var item = await _client.GetObjectInfoAsync(remotePath, false, token);
				_items.AddItem(new FtpListItemViewModel(_client, item, this, _transferService, _dialogService));
			}
		}
		#endregion

		#region can execute commands
		private bool CanExecuteRefreshCommand()
		{
			var isNotLoading = !IsLoading;
			var isDirectory = Type == FtpFileSystemObjectType.Directory;

			return isNotLoading && IsDirectory;
		}

		private bool CanExecuteUploadCommand()
		{
			var canWritePermission = (OwnerPermissions & FtpPermission.Write) == FtpPermission.Write;
			var isDirectory = Type == FtpFileSystemObjectType.Directory;
			var transferServiceExists = _transferService != null;

			return canWritePermission && IsDirectory && transferServiceExists;
		}

		private bool CanExecuteDownloadCommand()
		{
			var transferServiceExists = _transferService != null;
			var isFile = Type == FtpFileSystemObjectType.File;
			return transferServiceExists && isFile;
		}

		private bool CanExecuteDeleteCommand()
		{
			var canWritePermission = (OwnerPermissions & FtpPermission.Write) == FtpPermission.Write;
			var dialogServiceExists = _dialogService != null;
			return canWritePermission && dialogServiceExists;
		}

		private bool CanExecuteCreateFolderCommand(string folderName)
		{
			var canWritePermission = (OwnerPermissions & FtpPermission.Write) == FtpPermission.Write;
			var nameIsNoEmpty = !string.IsNullOrWhiteSpace(folderName);
			var nameIsValidPath = folderName?.IndexOfAny(Path.GetInvalidPathChars()) == -1;
			return canWritePermission && nameIsNoEmpty && nameIsValidPath;
		}		
		#endregion

		#region methods
		private async void UploadFinished(object recipient, object message)
		{
			try
			{
				if (message is ITransferItem transferItem)
				{
					var item = await _client.GetObjectInfoAsync(transferItem.RemotePath, false);
					if (item != null)
					{
						var search = _items
							.Select((_item, index) => (_item, index))
							.FirstOrDefault(x => x._item.FullName == transferItem.RemotePath);

						if(search == default)
						{
							await AccessUIAsync(() => _items.AddItem(new FtpListItemViewModel(_client, item, this, _transferService, _dialogService)));
						}												
						else
						{
							await AccessUIAsync(() => _items[search.index] = new FtpListItemViewModel(_client, item, this, _transferService, _dialogService));
						}
					}
				}
			}
			catch { }
		}
		#endregion
	}
}