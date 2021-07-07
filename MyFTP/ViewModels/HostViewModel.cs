using FluentFTP;
using MyFTP.Collections;
using MyFTP.Utils;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Utils.Comparers;
using Windows.System;

namespace MyFTP.ViewModels
{
	public class HostViewModel : BindableItem
	{
		private bool _isLoading;
		private IObservableSortedCollection<FtpListItemViewModel> _rootItems;

		public FtpClient Client { get; private set; }
		public bool IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }
		public ReadOnlyObservableCollection<FtpListItemViewModel> RootItems { get; }


		public HostViewModel(FtpClient client, DispatcherQueue dispatcher) : base(dispatcher)
		{
			Client = client;
			_rootItems = new ObservableSortedCollection<FtpListItemViewModel>(new FtpListItemComparer());
			RootItems = new ReadOnlyObservableCollection<FtpListItemViewModel>((ObservableCollection<FtpListItemViewModel>)_rootItems);
		}

		public async Task LoadRootItemsAsync(CancellationToken token = default)
		{
			IsLoading = true;
			try
			{
				var result = await Client.GetListingAsync("", token);
				_rootItems.Clear();
				foreach (var item in result)
				{
					_rootItems.AddItem(new FtpListItemViewModel(Client, item, Dispatcher));
					token.ThrowIfCancellationRequested();
				}
			}
			finally
			{
				IsLoading = false;
			}
		}

		public async Task DisconnectAsync() => await Client.DisconnectAsync();
	}
}