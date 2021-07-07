using MyFTP.ViewModels;
using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MyFTP.Views
{
	public sealed partial class HostViewPage : Page
	{
		public HostViewPage() => InitializeComponent();

		public HostViewModel ViewModel { get => (HostViewModel)GetValue(ViewModelProperty); set => SetValue(ViewModelProperty, value); }
		public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
			typeof(HostViewModel), typeof(HostViewPage), new PropertyMetadata(null));

		public FtpListItemViewModel SelectedItem { get => (FtpListItemViewModel)GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
		public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem",
			typeof(FtpListItemViewModel), typeof(HostViewPage), new PropertyMetadata(null, OnSelectedItemChanged));

		public bool ShowFolderOptions { get => (bool)GetValue(ShowFolderOptionsProperty); set => SetValue(ShowFolderOptionsProperty, value); }
		public static readonly DependencyProperty ShowFolderOptionsProperty = DependencyProperty.Register("ShowFolderOptions",
			typeof(bool), typeof(HostViewPage), new PropertyMetadata(false));

		private async static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var p = (HostViewPage)d;
			if (p.SelectedItem != null
				  && p.SelectedItem.IsDirectory
				  && !p.SelectedItem.IsLoading
				  && !p.SelectedItem.IsLoaded)
			{
				await p.SelectedItem.LoadItemsAsync();
			}
			p.ShowFolderOptions = p.SelectedItem?.IsDirectory == true;
		}

		public bool Theme
		{
			get => Frame.ActualTheme == ElementTheme.Dark;
			set => Frame.RequestedTheme = value ? ElementTheme.Dark : ElementTheme.Light;
		}

		protected async override void OnNavigatedTo(NavigationEventArgs args)
		{
			try
			{
				ViewModel = args.Parameter as HostViewModel;
				if (ViewModel == null)
					throw new InvalidOperationException("Invalid param");
				await ViewModel.LoadRootItemsAsync(default);
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

		private async void OnDisconnectButtonClick(Microsoft.UI.Xaml.Controls.SplitButton sender, Microsoft.UI.Xaml.Controls.SplitButtonClickEventArgs args)
		{
			await ViewModel.DisconnectAsync();
			Frame.GoBack();
		}

		private void ShowError(string message, Exception e = null)
		{
			infoBar.IsOpen = false;
			infoBar.Message = message;
			infoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
			infoBar.IsOpen = true;
			Debug.WriteLineIf(e != null, e);
		}
		private void ExitApp() => Application.Current.Exit();
	}
}