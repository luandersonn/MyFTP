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
			WeakReferenceMessenger.Default.Register<HostViewModel>(this, SuccessfulLogin);
		}
		public LoginViewModel ViewModel => (LoginViewModel)DataContext;
		private void SuccessfulLogin(object recipient, HostViewModel message)
		{
			Frame.Navigate(typeof(HostViewPage), message);
			WeakReferenceMessenger.Default.Unregister<HostViewModel>(this);
		}

		private void OnDeleteCredentialClicked(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var item = (FtpHostSettingsViewModel)button.DataContext;
			ViewModel.Delete(item);
		}
	}
}