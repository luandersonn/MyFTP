using Microsoft.Toolkit.Uwp.Helpers;
using MyFTP.Services;
using MyFTP.Utils;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;

namespace MyFTP.ViewModels
{
	public class SettingsViewModel : BindableItem
	{
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
		public ObservableCollection<FtpHostSettingsViewModel> FtpServerSettingsList { get; }
		#endregion

		#region constructor		
		public SettingsViewModel(ISettings settings, UpdateServiceViewModel updateService = null) : base(DispatcherQueue.GetForCurrentThread())
		{
			Settings = settings;
			UpdateService = updateService;
			FtpServerSettingsList = new ObservableCollection<FtpHostSettingsViewModel>();
			Task.Run(InitAsync);
		}
		#endregion

		#region methods		
		private async Task InitAsync()
		{
			var settings = await FtpHostSettings.GetAllAsync();
			await AccessUIAsync(async () =>
			{
				foreach (var (_, value) in settings)
				{
					var folder = await value.GetDefaultSaveLocationAsync();
					FtpServerSettingsList.Add(new FtpHostSettingsViewModel(value, folder));
				}
			});
		}
		#endregion
	}
}