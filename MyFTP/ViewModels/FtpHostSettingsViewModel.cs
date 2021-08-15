using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.Utils;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System;

namespace MyFTP.ViewModels
{
	public class FtpHostSettingsViewModel : BindableItem
	{
		#region fields	
		private readonly WeakReferenceMessenger _weakMessenger;
		#endregion

		#region properties
		public FtpHostSettings Item { get; }
		public string Id { get; }
		public string Host { get; }
		public int Port { get; }
		public string Username { get; }
		public StorageFolder Folder { get; private set; }
		public IAsyncRelayCommand ChangeFolderCommand { get; }
		public IAsyncRelayCommand OpenFolderCommand { get; }
		public IRelayCommand ResetFolderCommand { get; }
		public IAsyncRelayCommand DeleteSettingCommand { get; }
		#endregion

		#region constructor		
		public FtpHostSettingsViewModel(FtpHostSettings item, StorageFolder folder = null)
		{
			Item = item;
			Id = item.Id;
			Host = item.Host;
			Port = item.Port;
			Username = item.Username;
			Folder = folder;

			ChangeFolderCommand = new AsyncRelayCommand(ChangeFolderCommandAsync);
			OpenFolderCommand = new AsyncRelayCommand(OpenFolderCommandAsync);
			ResetFolderCommand = new RelayCommand(ResetFolder);
			DeleteSettingCommand = new AsyncRelayCommand(DeleteSettingCommandAsync);
			_weakMessenger = WeakReferenceMessenger.Default;			
		}
		#endregion

		#region methods		
		private async Task ChangeFolderCommandAsync()
		{
			var folder = await _weakMessenger.Send<RequestOpenFolderMessage>();
			if (folder != null)
			{
				try
				{
					Item.SetDefaultSaveLocation(folder);
					Folder = folder;
					OnPropertyChanged(nameof(Folder));
				}
				catch { }
			}
		}
		private async Task OpenFolderCommandAsync() => await Launcher.LaunchFolderAsync(Folder);
		private void ResetFolder()
		{
			Folder = ApplicationData.Current.TemporaryFolder;
			if (StorageApplicationPermissions.FutureAccessList.ContainsItem(Id))
				StorageApplicationPermissions.FutureAccessList.Remove(Id);
			OnPropertyChanged(nameof(Folder));
		}
		private async Task DeleteSettingCommandAsync()
		{
			await FtpHostSettings.DeleteAsync(Id);
		}
		#endregion
	}
}
