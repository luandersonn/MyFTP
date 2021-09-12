using MyFTP.ViewModels;
using System;
using System.IO;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace MyFTP.Controls
{
	public sealed partial class ReplaceItemDialog : ContentDialog
	{
		public ReplaceItemDialog(StorageFile newFile, ViewModels.FtpListItemViewModel itemToReplace)
		{
			this.InitializeComponent();
			NewFile = newFile ?? throw new ArgumentNullException(nameof(newFile));
			ItemToReplace = itemToReplace ?? throw new ArgumentNullException(nameof(itemToReplace));
			Opened += OnReplaceItemDialogOpened;
		}

		public BasicProperties NewFileProperties { get => (BasicProperties)GetValue(NewFilePropertiesProperty); set => SetValue(NewFilePropertiesProperty, value); }
		public static readonly DependencyProperty NewFilePropertiesProperty = DependencyProperty.Register("NewFileProperties",
				typeof(BasicProperties), typeof(ReplaceItemDialog), new PropertyMetadata(null));

		public StorageFile NewFile { get; }
		public FtpListItemViewModel ItemToReplace { get; }

		private async void OnReplaceItemDialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
		{
			StorageItemThumbnail thumbnail = await NewFile.GetThumbnailAsync(ThumbnailMode.PicturesView, 64, ThumbnailOptions.ResizeThumbnail);
			if (thumbnail != null)
			{
				thumbnail.Seek(0);
				var source = new BitmapImage
				{					
					DecodePixelType = DecodePixelType.Logical
				};
				newFileImage.Source = source;
				await source.SetSourceAsync(thumbnail);
			}

			NewFileProperties = await NewFile.GetBasicPropertiesAsync();
			
			var extesion = Path.GetExtension(ItemToReplace.Name);
			thumbnail = await Utils.IconHelper.GetFileIconAsync(extesion, 64);

			if (thumbnail != null)
			{
				thumbnail.Seek(0);
				var source = new BitmapImage
				{					
					DecodePixelType = DecodePixelType.Logical
				};
				itemToReplaceImage.Source = source;
				await source.SetSourceAsync(thumbnail);
			}
		}
	}
}
