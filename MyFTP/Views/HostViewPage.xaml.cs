using MyFTP.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;
namespace MyFTP.Views
{
	public sealed partial class HostViewPage : Page
	{
		public HostViewPage()
		{
			InitializeComponent();
			Crumbs = new ObservableCollection<FtpListItemViewModel>();
		}

		public HostViewModel ViewModel { get => (HostViewModel)GetValue(ViewModelProperty); set => SetValue(ViewModelProperty, value); }
		public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
			typeof(HostViewModel), typeof(HostViewPage), new PropertyMetadata(null));

		public FtpListItemViewModel SelectedItem { get => (FtpListItemViewModel)GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem",
			typeof(FtpListItemViewModel), typeof(HostViewPage), new PropertyMetadata(null, OnSelectedItemChanged));

		public bool ShowFolderOptions { get => (bool)GetValue(ShowFolderOptionsProperty); set => SetValue(ShowFolderOptionsProperty, value); }
		public static readonly DependencyProperty ShowFolderOptionsProperty = DependencyProperty.Register("ShowFolderOptions",
			typeof(bool), typeof(HostViewPage), new PropertyMetadata(false));

		public ObservableCollection<FtpListItemViewModel> Crumbs { get; }

		private async static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			var p = (HostViewPage)d;
			var item = (FtpListItemViewModel)p.treeView.SelectedItem;
			if (item != null && item.IsDirectory)
			{
				// Update the BreadcrumbBar
				p.Crumbs.Clear();
				var crumb = item;
				do
				{
					p.Crumbs.Insert(0, crumb);
					crumb = crumb.Parent;
				} while (crumb != null);

				if (!item.IsLoading && !item.IsLoaded)
				{
					try
					{
						await item.LoadItemsAsync();
					}
					catch (Exception e)
					{
						p.ShowError(e.Message, e);
					}
				}
			}
			p.ShowFolderOptions = p.SelectedItem?.IsDirectory == true;
		}		

		protected async override void OnNavigatedTo(NavigationEventArgs args)
		{
			try
			{
				ViewModel = args.Parameter as HostViewModel;
				if (ViewModel == null)
					throw new InvalidOperationException("Invalid param");
				await ViewModel.Root[0].LoadItemsAsync(default);
				treeView.SelectedItem = ViewModel.Root[0];
			}
			catch (Exception e)
			{
				ShowError(e.Message, e);
			}
		}

		private async void OnRefreshButtonClick(object sender, RoutedEventArgs args)
		{
			var button = (Control)sender;
			button.IsEnabled = false;
			try
			{
				await SelectedItem.LoadItemsAsync();
			}
			catch (Exception e)
			{
				ShowError(e.Message, e);
			}
			finally
			{
				button.IsEnabled = true;
			}
		}

		private void OnUploadFileButtonClick(object sender, RoutedEventArgs args)
		{
			throw new NotImplementedException();
		}

		private void OnDownloadFilesButtonClick(object sender, RoutedEventArgs args)
		{
			throw new NotImplementedException();
		}

		private async void OnDisconnectButtonClick(object sender, RoutedEventArgs args)
		{
			await ViewModel.DisconnectAsync();
			Frame.GoBack();
		}

		private void OnThemeRadioClicked(object sender, RoutedEventArgs e)
		{
			var item = (muxc.RadioMenuFlyoutItem)sender;
			Frame.RequestedTheme = (ElementTheme)Enum.Parse(typeof(ElementTheme), item.Tag.ToString());
		}

		private void ShowError(string message, Exception e = null)
		{
			infoBar.IsOpen = false;
			infoBar.Message = message;
			infoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
			infoBar.IsOpen = true;
			Debug.WriteLineIf(e != null, e);
		}

		private void OnBreadcrumbBarItemClicked(muxc.BreadcrumbBar sender, muxc.BreadcrumbBarItemClickedEventArgs args) => treeView.SelectedItem = args.Item;
		private void OnListViewItemClick(object sender, ItemClickEventArgs e) => treeView.SelectedItem = e.ClickedItem;
		
		private void OnListViewItemDoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
		{
			var frameworkElement = (FrameworkElement)sender;
			var item = (FtpListItemViewModel)frameworkElement.DataContext;
			treeView.SelectedItem = item;
		}

		private void OnListViewContextMenuClicked(object sender, RoutedEventArgs e) => throw new NotImplementedException();

		private void ExitApp() => Application.Current.Exit();		
	}
}