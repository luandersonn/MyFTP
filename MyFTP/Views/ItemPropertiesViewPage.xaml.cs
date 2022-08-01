using FluentFTP;
using MyFTP.ViewModels;
using System;
using System.IO;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;


namespace MyFTP.Views
{
	public sealed partial class ItemPropertiesViewPage : Page
	{
		public FtpListItemViewModel ViewModel { get => (FtpListItemViewModel)GetValue(ViewModelProperty); set => SetValue(ViewModelProperty, value); }
		public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
			typeof(FtpListItemViewModel), typeof(ItemPropertiesViewPage), new PropertyMetadata(null, ItemChanged));

		public ItemPropertiesViewPage()
		{
			InitializeComponent();
			Services.ISettings settings = (Services.ISettings)App.Current.Services.GetService(typeof(Services.ISettings));
			ElementTheme theme = ElementTheme.Default;
			settings?.TryGet("AppTheme", out theme);
			RequestedTheme = theme;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) => ViewModel = e.Parameter as FtpListItemViewModel;

		private static async void ItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var page = (ItemPropertiesViewPage)d;
			if (e.NewValue is FtpListItemViewModel m)
			{
				uint size = 80;
				StorageItemThumbnail thumbnail;
				var extension = Path.GetExtension(m.Name);
				if (m.Type == FluentFTP.FtpObjectType.Directory)
				{
					thumbnail = await Utils.IconHelper.GetFolderIconAsync(size);
				}
				else
				{
					thumbnail = await Utils.IconHelper.GetFileIconAsync(extension, size);
				}
				thumbnail.Seek(0);
				await page.image.SetSourceAsync(thumbnail);
				page.contentTypeTextBlock.Text = await Utils.IconHelper.GetContentType(extension);
			}
		}

		private bool CanRead(FtpPermission permission) => (permission & FtpPermission.Read) == FtpPermission.Read;
		private bool CanWrite(FtpPermission permission) => (permission & FtpPermission.Write) == FtpPermission.Write;
		private bool CanExecute(FtpPermission permission) => (permission & FtpPermission.Execute) == FtpPermission.Execute;
	}
}
