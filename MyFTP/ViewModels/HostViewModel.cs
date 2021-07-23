using FluentFTP;
using MyFTP.Collections;
using MyFTP.Utils;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Utils.Comparers;

namespace MyFTP.ViewModels
{
	public class HostViewModel : BindableItem
	{
		private IObservableSortedCollection<FtpListItemViewModel> _items;
		public IFtpClient Client { get; private set; }
		public ReadOnlyObservableCollection<FtpListItemViewModel> Root { get; }

		public HostViewModel(IFtpClient client)
		{
			Client = client;
			_items = new ObservableSortedCollection<FtpListItemViewModel>(new FtpListItemComparer());
			Root = new ReadOnlyObservableCollection<FtpListItemViewModel>((ObservableSortedCollection<FtpListItemViewModel>)_items);
			_items.AddItem(new FtpListItemViewModel(client, client.Host, ""));
		}

		public async Task DisconnectAsync() => await Client.DisconnectAsync();
	}
}