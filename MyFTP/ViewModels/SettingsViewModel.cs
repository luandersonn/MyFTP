using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Helpers;
using MyFTP.Services;
using MyFTP.Utils;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;

namespace MyFTP.ViewModels
{
	public class SettingsViewModel : BindableItem
	{
		#region constructor
		public SettingsViewModel(ISettings settings, UpdateServiceViewModel updateService = null) : base(DispatcherQueue.GetForCurrentThread())
		{
			Settings = settings;
			UpdateService = updateService;
			FtpHostSettingsList = new ObservableCollection<FtpHostSettingsViewModel>();
			LogFiles = new ObservableCollection<StorageFile>();
			RefreshHostSettingsCommand = new AsyncRelayCommand(RefreshHostSettingsAsync);
			RefreshLogsListCommand = new AsyncRelayCommand(RefreshLogsListAsync);
			OpenFileOnExploreCommand = new AsyncRelayCommand<StorageFile>(OpenFileOnExploreAsync, file => file != null);
		}
		#endregion

		#region properties
		public ISettings Settings { get; }
		public UpdateServiceViewModel UpdateService { get; }
		public SystemInformation SystemInformation => SystemInformation.Instance;
		public ElementTheme AppTheme
		{
			get
			{
				var theme = ElementTheme.Default;
				Settings.TryGet("AppTheme", out theme);
				return theme;
			}
			set
			{
				Settings.TrySet("AppTheme", value);
			}
		}
		public ObservableCollection<FtpHostSettingsViewModel> FtpHostSettingsList { get; }
		public ObservableCollection<StorageFile> LogFiles { get; }

		public IAsyncRelayCommand RefreshHostSettingsCommand { get; }
		public IAsyncRelayCommand RefreshLogsListCommand { get; }
		public IAsyncRelayCommand OpenFileOnExploreCommand { get; }
		#endregion

		#region methods
		private async Task RefreshHostSettingsAsync()
		{
			var settings = await FtpHostSettings.GetAllAsync();
			await AccessUIAsync(async () =>
				{
					FtpHostSettingsList.Clear();
					foreach (var (_, value) in settings)
					{
						var folder = await value.GetDefaultSaveLocationAsync();
						FtpHostSettingsList.Add(new FtpHostSettingsViewModel(value, folder));
					}
				});
		}
		private async Task RefreshLogsListAsync(CancellationToken token)
		{
			var folder = await StorageFolder.GetFolderFromPathAsync(LoggerFactory.LogDefaultFolderPath)
											.AsTask(token)
											.ConfigureAwait(false);

			var files = await folder.GetFilesAsync()
											.AsTask(token)
											.ConfigureAwait(false);

			await AccessUIAsync(() =>
			{
				LogFiles.Clear();
				foreach (var file in files)
				{
					LogFiles.Add(file);
				}
			});
		}

		private async Task OpenFileOnExploreAsync(StorageFile file)
		{
			if (file != null)
			{
				var parent = await file.GetParentAsync();
				var options = new FolderLauncherOptions();
				options.ItemsToSelect.Add(file);
				await Launcher.LaunchFolderAsync(parent, options);
			}
		}
		#endregion
	}
}