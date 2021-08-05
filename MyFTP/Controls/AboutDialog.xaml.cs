using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Helpers;
using MyFTP.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MyFTP.Controls
{
	public sealed partial class AboutDialog : ContentDialog
	{
		public AboutDialog()
		{
			this.InitializeComponent();

			Settings = App.Current.Services.GetService<ISettings>();
			ElementTheme theme = default;
			Settings?.TryGet<ElementTheme>("AppTheme", out theme);
			RequestedTheme = theme;
		}

		public ISettings Settings { get; }
		public SystemInformation SystemInformation => SystemInformation.Instance;
		public StoreService StoreService { get; }

		private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			args.Cancel = true;
			try
			{
				await SystemInformation.LaunchStoreForReviewAsync();
			}
			catch { }
		}
	}
}
