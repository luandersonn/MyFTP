using MyFTP.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using muxc = Microsoft.UI.Xaml.Controls;

namespace MyFTP.Controls
{
	public sealed partial class DeleteItemDialog : ContentDialog
	{
		public DeleteItemDialog() => InitializeComponent();
		public IEnumerable<FtpListItemViewModel> Items => DataContext as IEnumerable<FtpListItemViewModel>;

		private async void OnListViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.ItemContainer.ContentTemplateRoot is Grid root
							&& args.Item is FtpListItemViewModel item
							&& root.Children.OfType<Image>().FirstOrDefault() is Image image
							&& root.Children.OfType<muxc.ProgressRing>().FirstOrDefault() is muxc.ProgressRing progress)
			{

				if (args.InRecycleQueue)
				{
					image.Source = null;
				}
				else
				{
					args.Handled = true;
					switch (args.Phase)
					{
						case 0:
							args.RegisterUpdateCallback(1, OnListViewContainerContentChanging);
							break;

						case 1:

							var source = new BitmapImage
							{
								DecodePixelType = DecodePixelType.Logical
							};
							image.Source = source;
							StorageItemThumbnail thumbnail;
							try
							{
								switch (item.Type)
								{
									case FluentFTP.FtpObjectType.File:
										thumbnail = await Utils.IconHelper.GetFileIconAsync(Path.GetExtension(item.Name));
										break;
									case FluentFTP.FtpObjectType.Directory:
										thumbnail = await Utils.IconHelper.GetFolderIconAsync();
										break;
									default:
										return;
								}
								thumbnail.Seek(0);
								await source.SetSourceAsync(thumbnail);

								progress.IsActive = false;
							}
							catch (Exception e)
							{
								Debug.WriteLine(e);
							}
							break;
					}
				}
			}
		}
		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
		}
	}
}
