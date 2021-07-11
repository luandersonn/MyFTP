using FluentFTP;
using Humanizer;
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
		private IObservableSortedCollection<FtpListItemViewModel> _items;
		private bool _isLoaded;
		private bool _isLoading;
		private readonly FtpListItem _ftpItem;
		private readonly IFtpClient _client;

		#region constructor		
		public FtpListItemViewModel(IFtpClient client, string name, string fullName, DispatcherQueue dispatcher) : base(dispatcher)
		{
			_client = client;
			Name = name;
			FullName = fullName;
			_items = new ObservableSortedCollection<FtpListItemViewModel>(new FtpListItemComparer());
			Items = new ReadOnlyObservableCollection<FtpListItemViewModel>((ObservableCollection<FtpListItemViewModel>)_items);
			_isLoaded = IsLoading = false;
			Type = FtpFileSystemObjectType.Directory;
		}

		public FtpListItemViewModel(IFtpClient client, FtpListItem item, DispatcherQueue dispatcher) : this(client, item.Name, item.FullName, dispatcher)
		{
			_ftpItem = item ?? throw new ArgumentNullException(nameof(item));			
			Type = item.Type;
			SubType = item.SubType;
			Size = item.Size < 0 ? "" : item.Size.Bytes().ToString("#.##");
			Modified = item.Modified.Humanize();
		}
		public FtpListItemViewModel(IFtpClient client, FtpListItem item, FtpListItemViewModel parent, DispatcherQueue dispatcher) : this(client, item, dispatcher)
		{
			Parent = parent;
		}
		#endregion

		public string Name { get; }
		public string FullName { get; }
		public FtpListItemViewModel Parent { get; }
		public FtpFileSystemObjectType Type { get; }
		public FtpFileSystemObjectSubType SubType { get; }
		public bool IsDirectory => Type == FtpFileSystemObjectType.Directory;
		public string Size { get; }
		public string Modified { get; }
		public ReadOnlyObservableCollection<FtpListItemViewModel> Items { get; }
		public bool IsLoaded { get => _isLoaded; private set => Set(ref _isLoaded, value); }
		public bool IsLoading { get => _isLoading; private set => Set(ref _isLoading, value); }


		public async Task LoadItemsAsync(CancellationToken token = default)
		{
			await AccessUIAsync(() => IsLoading = true);
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
						_items.AddItem(new FtpListItemViewModel(_client, item, this, Dispatcher));
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
			}
		}
	}
}