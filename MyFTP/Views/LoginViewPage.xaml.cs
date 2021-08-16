using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MyFTP.Views
{
	public sealed partial class LoginViewPage : Page
	{
		public LoginViewPage()
		{
			InitializeComponent();
			this.DataContext = App.Current.Services.GetService<LoginViewModel>();
			Loaded += (sender, args) => WeakReferenceMessenger.Default.Register<FtpListItemViewModel>(this, SuccessfulLogin);
			Unloaded += (sender, args) => WeakReferenceMessenger.Default.Unregister<FtpListItemViewModel>(this);
		}
		public LoginViewModel ViewModel => (LoginViewModel)DataContext;
		private void SuccessfulLogin(object recipient, FtpListItemViewModel message)
		{
			Frame.Navigate(typeof(HostViewPage), message);			
		}

		private void OnDeleteCredentialClicked(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var item = (FtpHostSettingsViewModel)button.DataContext;
			ViewModel.Delete(item);
		}

		private void GoToSettings() => Frame.Navigate(typeof(SettingsViewPage));
	}
}