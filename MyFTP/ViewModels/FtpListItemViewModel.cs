using FluentFTP;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.Collections;
using MyFTP.Services;
using MyFTP.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils.Comparers;
using Windows.Storage;
using Windows.System;

namespace MyFTP.ViewModels
{
	public class FtpListItemViewModel : BindableItem, IDragTarget, IDropTarget
	{
		#region fields		
		private readonly IObservableSortedCollection<FtpListItemViewModel> _items;
		private bool _isLoaded;
		private bool _isLoading;
		private bool _isRenameDialogOpen;
		private bool _isRenaming;
		private FtpPermission _ownerPermissions;
		private string _name;
		private FtpListItemViewModel _parent;
		private readonly FtpListItem _ftpItem;
		private readonly WeakReferenceMessenger _weakMessenger;
		private readonly ITransferItemService _transferService;
		private readonly IDialogService _dialogService;
		private readonly string _guid;
		#endregion

		#region constructor
		public FtpListItemViewModel(IFtpClient client, FtpListItem item, FtpListItemViewModel parent, ITransferItemService transferService, IDialogService dialogService)
		{
			Client = client ?? throw new ArgumentNullException(nameof(client));
			Parent = parent;

			_items = new ObservableSortedCollection<FtpListItemViewModel>(new FtpListItemComparer());
			Items = new ReadOnlyObservableCollection<FtpListItemViewModel>((ObservableCollection<FtpListItemViewModel>)_items);

			_isLoaded = _isLoading = false;

			RefreshCommand = new AsyncRelayCommand(RefreshCommandAsync, CanExecuteRefreshCommand);
			UploadFilesCommand = new AsyncRelayCommand(UploadFilesCommandAsync, CanExecuteUploadCommand);
			UploadFolderCommand = new AsyncRelayCommand(UploadFolderCommandAsync, CanExecuteUploadCommand);
			DownloadCommand = new AsyncRelayCommand<IEnumerable<FtpListItemViewModel>>(DownloadCommandAsync, CanExecuteDownloadCommand);
			DeleteCommand = new AsyncRelayCommand<IEnumerable<FtpListItemViewModel>>(DeleteCommandAsync, CanExecuteDeleteCommand);
			OpenRenameDialogCommand = new AsyncRelayCommand(OpenRenameDialogCommandAsync, CanExecuteOpenRenameDialogCommand);
			RenameCommand = new AsyncRelayCommand<string>(RenameCommandAsync, CanExecuteRenameCommand);
			CreateFolderCommand = new AsyncRelayCommand<string>(CreateFolderCommandAsync, CanExecuteCreateFolderCommand);


			Dispatcher = DispatcherQueue.GetForCurrentThread();

			_weakMessenger = WeakReferenceMessenger.Default;

			_transferService = transferService;
			_dialogService = dialogService;
			_guid = Guid.NewGuid().ToString();
			_weakMessenger.Register<object, string>(this, _guid, UploadFinished);

			Client.EnableThreadSafeDataConnections = true;

			_ftpItem = item ?? throw new ArgumentNullException(nameof(item));

			if (item.Name == "/")
				Name = client.Host;
			else
				Name = item.Name;
			Type = item.Type;
			SubType = item.SubType;
			Size = item.Size;
			Modified = item.Modified;
			OwnerPermissions = item.OwnerPermissions;
		}
		#endregion

		#region properties
		public IFtpClient Client { get; }
		public string Name { get => _name; private set => Set(ref _name, value); }
		public string FullName
		{
			get
			{
				if (Parent == null)
					return "/";
				else
					return Parent.FullName + "/" + Name;
			}
		}
		public FtpPermission OwnerPermissions { get => _ownerPermissions; private set => Set(ref _ownerPermissions, value); }
		public FtpListItemViewModel Parent { get => _parent; private set => Set(ref _parent, value); }
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
		public IAsyncRelayCommand UploadFilesCommand { get; }
		public IAsyncRelayCommand UploadFolderCommand { get; }
		public IAsyncRelayCommand<IEnumerable<FtpListItemViewModel>> DownloadCommand { get; }
		public IAsyncRelayCommand<IEnumerable<FtpListItemViewModel>> DeleteCommand { get; }
		public IAsyncRelayCommand OpenRenameDialogCommand { get; }
		public IAsyncRelayCommand<string> RenameCommand { get; }
		public IAsyncRelayCommand<string> CreateFolderCommand { get; }

		public async Task RefreshCommandAsync(CancellationToken token = default)
		{
			IsLoading = true;
			RefreshCommand.NotifyCanExecuteChanged();
			try
			{
				if (Type != FtpFileSystemObjectType.Directory)
					throw new NotSupportedException();
				// Load the root permission manually
				var result = await Client.GetListingAsync(FullName, token);

				_items.Clear();
				foreach (var item in result)
				{
					_items.AddItem(new FtpListItemViewModel(Client, item, this, _transferService, _dialogService));
				}
				IsLoaded = true;
			}
			catch (Exception e)
			{
				IsLoaded = false;
				_weakMessenger.Send<ErrorMessage>(new ErrorMessage(e));
			}
			finally
			{
				IsLoading = false;
				RefreshCommand.NotifyCanExecuteChanged();
			}
		}
		private async Task UploadFilesCommandAsync(CancellationToken token)
		{
			try
			{
				var files = await _weakMessenger.Send<RequestOpenFilesMessage>();
				if (files != null)
				{
					foreach (var file in files)
					{
						bool result = true;
						var remotePath = string.Format("{0}/{1}", FullName, file.Name);
						if (_dialogService != null && await Client.GetObjectInfoAsync(remotePath, token: token) is FtpListItem current)
						{
							result = await _dialogService.AskForReplaceAsync(file, new FtpListItemViewModel(Client, current, this, null, null));
						}
						if (result)
							_transferService.EnqueueUpload(Client, remotePath, file, _guid);
					}
				}
			}
			catch (Exception e)
			{
				_weakMessenger.Send<ErrorMessage>(new ErrorMessage(e));
			}
		}
		private async Task UploadFolderCommandAsync(CancellationToken token)
		{
			try
			{
				var folder = await _weakMessenger.Send<RequestOpenFolderMessage>();
				if (folder != null)
				{
					var remotePath = string.Format("{0}/{1}", FullName, folder.Name);
					_transferService.EnqueueUpload(Client, remotePath, folder, _guid);
				}
			}
			catch (Exception e)
			{
				_weakMessenger.Send<ErrorMessage>(new ErrorMessage(e));
			}
		}
		private async Task DownloadCommandAsync(IEnumerable<FtpListItemViewModel> arg, CancellationToken token)
		{
			try
			{
				if (arg != null && arg.Any())
				{
					arg = arg.ToList(); // Force linq execution
					var folder = await _weakMessenger.Send<RequestOpenFolderMessage>();
					if (folder != null)
					{
						foreach (var item in arg)
						{
							if (item.Type == FtpFileSystemObjectType.Directory)
							{
								var _foder = await folder.CreateFolderAsync(item.Name, CreationCollisionOption.OpenIfExists);
								_transferService.EnqueueDownload(item.Client, item.FullName, _foder);
							}
							else
							{
								var _file = await folder.CreateFileAsync(item.Name, CreationCollisionOption.ReplaceExisting);
								_transferService.EnqueueDownload(item.Client, item.FullName, _file);
							}
						}
					}
				}
				else if (Type == FtpFileSystemObjectType.Directory)
				{
					var folder = await _weakMessenger.Send<RequestOpenFolderMessage>();
					if (folder != null)
					{
						_transferService.EnqueueDownload(Client, FullName, folder);
					}
				}
				else
				{
					var file = await _weakMessenger.Send<RequestSaveFileMessage>(new RequestSaveFileMessage() { FileNameSuggestion = Name });
					if (file != null)
					{
						_transferService.EnqueueDownload(Client, FullName, file);
					}
				}
			}
			catch (Exception e)
			{
				_weakMessenger.Send<ErrorMessage>(new ErrorMessage(e));
			}
		}
		private async Task DeleteCommandAsync(IEnumerable<FtpListItemViewModel> arg, CancellationToken token)
		{
			try
			{
				if (arg != null && arg.Any())
				{
					arg = arg.ToList(); // Force linq execution
					// Delete collection of items
					if (await _dialogService.AskForDeleteAsync(arg))
					{
						foreach (var item in arg)
						{
							try
							{
								if (item.Type == FtpFileSystemObjectType.Directory)
									await item.Client.DeleteDirectoryAsync(item.FullName, token);
								else
									await item.Client.DeleteFileAsync(item.FullName, token);
								if (item.Parent != null)
									item.Parent._items.RemoveItem(item);
							}
							catch(Exception e)
							{
								_weakMessenger.Send<ErrorMessage>(new ErrorMessage(e));
							}
						}
					}
				}
				else // Delete self
				{
					if (await _dialogService.AskForDeleteAsync(new FtpListItemViewModel[] { this }))
					{
						if (Type == FtpFileSystemObjectType.Directory)
							await Client.DeleteDirectoryAsync(FullName, token);
						else
							await Client.DeleteFileAsync(FullName, token);
						if (Parent != null)
							Parent._items.RemoveItem(this);
					}
				}
			}
			catch (Exception e)
			{
				_weakMessenger.Send<ErrorMessage>(new ErrorMessage(e));
			}
		}

		private async Task OpenRenameDialogCommandAsync(CancellationToken token)
		{
			try
			{
				_isRenameDialogOpen = true;
				OpenRenameDialogCommand.NotifyCanExecuteChanged();
				await _dialogService.OpenRenameDialogAsync(RenameCommand, Name);
			}
			catch (Exception e)
			{
				_weakMessenger.Send<ErrorMessage>(new ErrorMessage(e));
			}
			finally
			{
				_isRenameDialogOpen = false;
				OpenRenameDialogCommand.NotifyCanExecuteChanged();
			}
		}

		private async Task RenameCommandAsync(string newItemName, CancellationToken token)
		{
			try
			{
				_isRenaming = true;
				RenameCommand.NotifyCanExecuteChanged();
				var newRemotePath = FullName.Substring(0, FullName.Length - Name.Length) + newItemName;
				if (await Client.DirectoryExistsAsync(newRemotePath, token) || await Client.FileExistsAsync(newRemotePath))
					throw new FtpException("This name is already used by a directory or file");
				await Client.RenameAsync(FullName, newRemotePath, token: token);
				Name = newItemName;
				OnPropertyChanged(nameof(FullName));
			}
			catch (Exception e)
			{
				_weakMessenger.Send<ErrorMessage>(new ErrorMessage(e));
				throw;
			}
			finally
			{
				_isRenaming = false;
				RenameCommand.NotifyCanExecuteChanged();
			}
		}

		private async Task CreateFolderCommandAsync(string folderName, CancellationToken token)
		{
			try
			{
				var remotePath = string.Format("{0}/{1}", FullName, folderName);
				if (await Client.CreateDirectoryAsync(remotePath, false, token))
				{
					var item = await Client.GetObjectInfoAsync(remotePath, false, token);
					_items.AddItem(new FtpListItemViewModel(Client, item, this, _transferService, _dialogService));
				}
			}
			catch (Exception e)
			{
				_weakMessenger.Send<ErrorMessage>(new ErrorMessage(e));
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

		private bool CanExecuteDownloadCommand(IEnumerable<FtpListItemViewModel> arg)
		{
			var transferServiceExists = _transferService != null;
			var containsItem = arg?.Any() == true;
			return transferServiceExists && (containsItem || arg is null);
		}

		private bool CanExecuteDeleteCommand(IEnumerable<FtpListItemViewModel> arg)
		{
			var canWritePermission = (OwnerPermissions & FtpPermission.Write) == FtpPermission.Write;
			var dialogServiceExists = _dialogService != null;
			var containsItem = arg?.Any() == true;
			return canWritePermission && dialogServiceExists && (containsItem || arg is null);
		}

		private bool CanExecuteOpenRenameDialogCommand()
		{
			var canWritePermission = (OwnerPermissions & FtpPermission.Write) == FtpPermission.Write;
			var dialogServiceExists = _dialogService != null;
			var renameDialogIsClosed = !_isRenameDialogOpen;
			return canWritePermission && dialogServiceExists && renameDialogIsClosed;
		}

		private bool CanExecuteRenameCommand(string itemName)
		{
			var canWritePermission = (OwnerPermissions & FtpPermission.Write) == FtpPermission.Write;
			var nameIsNoEmpty = !string.IsNullOrWhiteSpace(itemName);
			var nameIsValidPath = Type == FtpFileSystemObjectType.Directory
				? itemName?.IndexOfAny(Path.GetInvalidPathChars()) == -1
				: itemName?.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;
			var notEqualsToCurrent = itemName != Name;
			var isNotRenaming = !_isRenaming;

			return canWritePermission && nameIsNoEmpty && nameIsValidPath && notEqualsToCurrent && isNotRenaming;
		}

		private bool CanExecuteCreateFolderCommand(string folderName)
		{
			var canWritePermission = (OwnerPermissions & FtpPermission.Write) == FtpPermission.Write;
			var nameIsNoEmpty = !string.IsNullOrWhiteSpace(folderName);
			var nameIsValidPath = folderName?.IndexOfAny(Path.GetInvalidPathChars()) == -1;
			var isDirectory = Type == FtpFileSystemObjectType.Directory;
			return canWritePermission && nameIsNoEmpty && nameIsValidPath && isDirectory;
		}
		#endregion

		#region methods
		private async void UploadFinished(object recipient, object message)
		{
			try
			{
				if (message is ITransferItem transferItem)
				{
					var item = await Client.GetObjectInfoAsync(transferItem.RemotePath, false);
					if (item != null)
					{
						var search = _items
							.Select((_item, index) => (_item, index))
							.FirstOrDefault(x => x._item.FullName == transferItem.RemotePath);

						if (search == default)
						{
							await AccessUIAsync(() => _items.AddItem(new FtpListItemViewModel(Client, item, this, _transferService, _dialogService)));
						}
						else
						{
							await AccessUIAsync(() => _items[search.index] = new FtpListItemViewModel(Client, item, this, _transferService, _dialogService));
						}
					}
				}
			}
			catch { }
		}

		public async void DropItems(IEnumerable<IDragTarget> items)
		{
			int success = 0, error = 0;
			foreach (var item in items.Cast<FtpListItemViewModel>())
			{
				try
				{
					var newRemotePath = string.Format("{0}/{1}", FullName, item.Name);
					bool hasSuccess = false;
					switch (item.Type)
					{
						case FtpFileSystemObjectType.File:
							hasSuccess = await Client.MoveFileAsync(item.FullName, newRemotePath, FtpRemoteExists.Skip, default);
							break;

						case FtpFileSystemObjectType.Directory:
							hasSuccess = await Client.MoveDirectoryAsync(item.FullName, newRemotePath, FtpRemoteExists.Skip, default);
							break;
					}

					if (hasSuccess)
					{
						success++;
						item.Parent?._items.RemoveItem(item);
						item.Parent = this;
						OnPropertyChanged(FullName);
						_items.AddItem(item);
					}
					else
					{
						error++;
					}
				}
				catch
				{
					error++;
				}
			}

			if (error != 0)
				_weakMessenger.Send(new ErrorMessage(new Exception($"{error} items cannot be moved")));
		}

		public void DropItems(IReadOnlyList<IStorageItem> items)
		{
			foreach (var item in items)
			{
				var remotePath = string.Format("{0}/{1}", FullName, item.Name);
				if (item.IsOfType(StorageItemTypes.Folder))
				{
					_transferService.EnqueueUpload(Client, remotePath, (StorageFolder)item, _guid);
				}
				else
				{
					_transferService.EnqueueUpload(Client, remotePath, (StorageFile)item, _guid);
				}
			}
		}

		public bool IsDragItemSupported(IDragTarget item) => item.GetType() == typeof(FtpListItemViewModel) && item != this;

		#endregion
	}
}