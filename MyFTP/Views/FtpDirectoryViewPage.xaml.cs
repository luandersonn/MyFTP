using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.Utils;
using MyFTP.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace MyFTP.Views
{
	public sealed partial class FtpDirectoryViewPage : Page
	{
		public FtpDirectoryViewPage()
		{
			this.InitializeComponent();
			Loaded += (sender, args) =>
			{
				WeakReferenceMessenger.Default.Register<SelectedItemChangedMessage<FtpListItemViewModel>>(this, OnViewModelChanged);
			};
			Unloaded += (sender, args) =>
			{
				WeakReferenceMessenger.Default.Unregister<SelectedItemChangedMessage<FtpListItemViewModel>>(this);
			};
		}

		public FtpListItemViewModel ViewModel { get => (FtpListItemViewModel)GetValue(ViewModelProperty); set => SetValue(ViewModelProperty, value); }
		public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
			typeof(FtpListItemViewModel), typeof(FtpDirectoryViewPage), new PropertyMetadata(null));

		private async void OnViewModelChanged(object recipient, SelectedItemChangedMessage<FtpListItemViewModel> message)
		{
			ViewModel = message.Item;
			if (ViewModel != null && !ViewModel.IsLoaded && ViewModel.RefreshCommand.CanExecute(null))
			{
				await ViewModel.RefreshCommand.ExecuteAsync(null);
			}
		}

		private async void OnListViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.ItemContainer.ContentTemplateRoot is Grid root
							&& args.Item is FtpListItemViewModel item
							&& root.Children.OfType<Image>().FirstOrDefault() is Image image)
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
									case FluentFTP.FtpFileSystemObjectType.File:
										thumbnail = await Utils.IconHelper.GetFileIconAsync(Path.GetExtension(item.Name));
										break;
									case FluentFTP.FtpFileSystemObjectType.Directory:
										thumbnail = await Utils.IconHelper.GetFolderIconAsync();
										break;
									default:
										return;
								}
								thumbnail.Seek(0);
								await source.SetSourceAsync(thumbnail);
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

		private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//var list = (ListView)sender;
			//DownloadCommandButton.CommandParameter = list.SelectedItems.Cast<FtpListItemViewModel>();
		}
		private void OnListViewItemClick(object sender, ItemClickEventArgs e)
		{
			//	treeView.SelectedItem = e.ClickedItem;
		}

		private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			/*var control = (Control)sender;
			var item = (FtpListItemViewModel)control.DataContext;
			if (item.Type == FluentFTP.FtpFileSystemObjectType.Directory)
				treeView.SelectedItem = item;
			else
				throw new NotImplementedException();*/
		}
		private void OnListViewItemDoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
		{
			var frameworkElement = (FrameworkElement)sender;
			var item = (FtpListItemViewModel)frameworkElement.DataContext;
			WeakReferenceMessenger.Default.Send(new SelectedItemChangedMessage<FtpListItemViewModel>(item));
		}

		private void FocusNewFolderTextBox()
		{
			//createFolderTextbox.Focus(FocusState.Programmatic);
		}
	}
}
