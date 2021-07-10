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

		public FtpListItemViewModel(IFtpClient client, FtpListItem item, DispatcherQueue dispatcher) : base(dispatcher)
		{
			Client = client;
			FtpItem = item ?? throw new ArgumentNullException(nameof(item));
			_items = new ObservableSortedCollection<FtpListItemViewModel>(new FtpListItemComparer());
			Items = new ReadOnlyObservableCollection<FtpListItemViewModel>((ObservableCollection<FtpListItemViewModel>)_items);
			_isLoaded = IsLoading = false;
		}

		public IFtpClient Client { get; }
		public FtpListItem FtpItem { get; }
		public FtpFileSystemObjectType Type => FtpItem.Type;
		public FtpFileSystemObjectSubType SubType => FtpItem.SubType;
		public bool IsDirectory => Type == FtpFileSystemObjectType.Directory;
		public string Size => FtpItem.Size < 0 ? "" : FtpItem.Size.Bytes().ToString("#.##");
		public string Modified => FtpItem.Modified.Humanize();
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
				var result = await Client.GetListingAsync(FtpItem.FullName, token);
				await AccessUIAsync(() =>
				{
					_items.Clear();
					foreach (var item in result)
					{
						_items.AddItem(new FtpListItemViewModel(Client, item, Dispatcher));
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