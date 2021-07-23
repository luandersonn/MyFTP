using FluentFTP;
using Microsoft.Toolkit.Mvvm.Input;
using MyFTP.Collections;
using MyFTP.Utils;
using System;
using System.Collections.ObjectModel;
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
		#endregion

		#region constructor
		private FtpListItemViewModel(IFtpClient client, FtpListItemViewModel parent)
		{
			_client = client ?? throw new ArgumentNullException(nameof(client));
			Parent = parent;

			_items = new ObservableSortedCollection<FtpListItemViewModel>(new FtpListItemComparer());
			Items = new ReadOnlyObservableCollection<FtpListItemViewModel>((ObservableCollection<FtpListItemViewModel>)_items);

			_isLoaded = _isLoading = false;

			RefreshCommand = new AsyncRelayCommand(LoadItemsAsync, () => !IsLoading && Type == FtpFileSystemObjectType.Directory);

			Dispatcher = DispatcherQueue.GetForCurrentThread();
		}
		public FtpListItemViewModel(IFtpClient client, string name, string fullName) : this(client, null)
		{
			Name = name;
			FullName = fullName;
			Type = FtpFileSystemObjectType.Directory;
		}

		public FtpListItemViewModel(IFtpClient client, FtpListItem item, FtpListItemViewModel parent) : this(client, parent)
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
		#endregion

		#region methods		
		public async Task LoadItemsAsync(CancellationToken token = default)
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
						_items.AddItem(new FtpListItemViewModel(_client, item, this));
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
		#endregion
	}
}