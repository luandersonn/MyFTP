using FluentFTP;
using MyFTP.Collections;
using MyFTP.Utils;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Utils.Comparers;
using Windows.System;

namespace MyFTP.ViewModels
{
	public class HostViewModel : BindableItem
	{
		private bool _isLoading;
		private IObservableSortedCollection<FtpListItemViewModel> _items;
		public FtpClient Client { get; private set; }
		public bool IsLoading { get => _isLoading; set => Set(ref _isLoading, value); }
		public ReadOnlyObservableCollection<FtpListItemViewModel> Root { get; }

		public HostViewModel(FtpClient client, DispatcherQueue dispatcher) : base(dispatcher)
		{
			Client = client;
			_items = new ObservableSortedCollection<FtpListItemViewModel>(new FtpListItemComparer());
			Root = new ReadOnlyObservableCollection<FtpListItemViewModel>((ObservableSortedCollection<FtpListItemViewModel>)_items);
			_items.AddItem(new FtpListItemViewModel(client, client.Host, "", dispatcher));
		}

		public async Task DisconnectAsync() => await Client.DisconnectAsync();
	}
}