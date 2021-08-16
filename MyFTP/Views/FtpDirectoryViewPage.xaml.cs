using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using MyFTP.Services;
using MyFTP.Utils;
using MyFTP.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace MyFTP.Views
{
	public sealed partial class FtpDirectoryViewPage : Page
	{
		public FtpListItemViewModel ViewModel { get => (FtpListItemViewModel)GetValue(ViewModelProperty); set => SetValue(ViewModelProperty, value); }
		public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
			typeof(FtpListItemViewModel), typeof(FtpDirectoryViewPage), new PropertyMetadata(null));

		public IEnumerable<FtpListItemViewModel> SelectedItems { get => (IEnumerable<FtpListItemViewModel>)GetValue(SelectedItemsProperty); set => SetValue(SelectedItemsProperty, value); }
		public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register("SelectedItems",
				typeof(IEnumerable<FtpListItemViewModel>), typeof(FtpDirectoryViewPage), new PropertyMetadata(null));

		public FtpDirectoryViewPage()
		{
			this.InitializeComponent();
			SelectedItems = Enumerable.Empty<FtpListItemViewModel>();
			Loaded += (sender, args) =>
			{
				WeakReferenceMessenger.Default.Register<SelectedItemChangedMessage<FtpListItemViewModel>>(this, OnViewModelChanged);
			};
			Unloaded += (sender, args) =>
			{
				WeakReferenceMessenger.Default.Unregister<SelectedItemChangedMessage<FtpListItemViewModel>>(this);
			};
		}


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
		private void FocusNewFolderTextBox() => createFolderTextbox.Focus(FocusState.Programmatic);

		private void SelectAll()
		{
			if (SelectedItems.Count() == ViewModel.Items.Count)
				itemsListView.DeselectAll();
			else
				itemsListView.SelectAllSafe();
		}

		private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var list = (ListView)sender;
			SelectedItems = list.SelectedItems.Cast<FtpListItemViewModel>();
		}

		private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
		{
			var control = (Control)sender;
			var item = (FtpListItemViewModel)control.DataContext;


			//TODO: Remove this from final version		
			var transferService = App.Current.Services.GetService<Services.ITransferItemService>();
			if (item.Type == FluentFTP.FtpFileSystemObjectType.File && transferService != null)
			{
				await OpenFileAsync(item, transferService);
			}
			else
			{
				WeakReferenceMessenger.Default.Send(new SelectedItemChangedMessage<FtpListItemViewModel>(this, item));
			}
		}
		private async void OnListViewItemDoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
		{
			var frameworkElement = (FrameworkElement)sender;
			var item = (FtpListItemViewModel)frameworkElement.DataContext;
			//TODO: Remove this from final version		
			var transferService = App.Current.Services.GetService<Services.ITransferItemService>();
			if (item.Type == FluentFTP.FtpFileSystemObjectType.File && transferService != null)
			{
				await OpenFileAsync(item, transferService);
			}
			else
			{
				WeakReferenceMessenger.Default.Send(new SelectedItemChangedMessage<FtpListItemViewModel>(this, item));
			}
		}
		//TODO: REMOVE THIS
		// WORKAROUND FOR NOW
		private async Task OpenFileAsync(FtpListItemViewModel item, ITransferItemService transferService)
		{
			var progressBar = new Microsoft.UI.Xaml.Controls.ProgressBar
			{
				Maximum = 1,
				IsIndeterminate = true
			};
			var source = new CancellationTokenSource();
			var dialog = new ContentDialog
			{
				Title = "Downloading",
				Content = progressBar,
				CloseButtonText = "Cancel",
				RequestedTheme = RequestedTheme
			};
			var progress = new Progress<double>(x => progressBar.Value = x);
			var file = await Utils.FileHelper.CreateTempFileAsync(Path.GetExtension(item.FullName));
			dialog.Opened += async (s, args) =>
			{
				try
				{
					await Task.Delay(TimeSpan.FromSeconds(1));
					progressBar.IsIndeterminate = false;
					await transferService.DownloadAsync(item.Client, item.FullName, file, progress, source.Token);
					await Launcher.LaunchFileAsync(file);
				}
				catch
				{
					await file.DeleteAsync(Windows.Storage.StorageDeleteOption.PermanentDelete);
				}
				finally
				{
					dialog.Hide();
				}
			};
			dialog.CloseButtonClick += (s, args) => source.Cancel();

			await dialog.ShowAsync();
		}

		private void ShowItemDropArea()
		{
			fadeIn.Start();			
			ItemDropArea.IsHitTestVisible = true;
		}

		private void HideItemDropArea()
		{
			fadeOut.Start();			
			ItemDropArea.IsHitTestVisible = false;
		}
	}
}
