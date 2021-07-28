using System;
using System.IO;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace MyFTP.Controls
{
	public sealed partial class DeleteItemDialog : ContentDialog
	{
		public DeleteItemDialog()
		{
			this.InitializeComponent();
			Opened += OnDeleteItemDialogOpened;
		}

		private async void OnDeleteItemDialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
		{
			try
			{
				if (ViewModel != null)
				{
					StorageItemThumbnail thumbnail = null;
					if (ViewModel.Type == FluentFTP.FtpFileSystemObjectType.File)
					{
						var extesion = Path.GetExtension(ViewModel.Name);
						thumbnail = await Utils.IconHelper.GetFileIconAsync(extesion, 128);
					}
					else
					{
						thumbnail = await Utils.IconHelper.GetFolderIconAsync(128);
					}
					if (thumbnail != null)
					{
						thumbnail.Seek(0);
						var source = new BitmapImage
						{
							DecodePixelWidth = 100,
							DecodePixelHeight = 100,
							DecodePixelType = DecodePixelType.Logical
						};
						image.Source = source;
						await source.SetSourceAsync(thumbnail);
					}
				}
			}
			finally
			{
				progress.IsActive = false;
			}
		}

		public ViewModels.FtpListItemViewModel ViewModel => DataContext as ViewModels.FtpListItemViewModel;

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
		}
	}
}
