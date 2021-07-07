using MyFTP.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace MyFTP.Views
{
	public sealed partial class LoginViewPage : Page
	{
		public LoginViewPage()
		{
			InitializeComponent();
			ViewModel = new LoginViewModel(Windows.System.DispatcherQueue.GetForCurrentThread());
		}

		public LoginViewModel ViewModel { get; }

		private bool ShowError
		{
			get => infoBar.IsOpen || loginProgress.ShowError;
			set => infoBar.IsOpen = loginProgress.ShowError = value;
		}

		private async void LoginButtonClick(object sender, RoutedEventArgs args)
		{
			var button = (Button)sender;
			button.IsEnabled = false;
			loginProgress.IsIndeterminate = true;
			ShowError = false;
			try
			{
				await Task.Delay(200);
				var result = await ViewModel.ConnectAsync();
				Frame.Navigate(typeof(Views.HostViewPage), result);
			}
			catch (Exception e)
			{
				infoBar.Message = e.Message;
				ShowError = true;
			}
			finally
			{
				button.IsEnabled = true;
			}
		}		
	}
}