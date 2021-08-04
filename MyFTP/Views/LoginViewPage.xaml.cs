using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.ViewModels;
using System;
using System.Linq;
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

		private void OnUsernameTextboxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.CheckCurrent() && args.Reason != AutoSuggestionBoxTextChangeReason.SuggestionChosen)
			{
				if (string.IsNullOrWhiteSpace(sender.Text))
					sender.ItemsSource = ViewModel.SavedCredentialsList;
				else
					sender.ItemsSource = ViewModel
								.SavedCredentialsList
								.Where(credential => credential.StartsWith(sender.Text, StringComparison.InvariantCultureIgnoreCase));
			}
		}

		private void AutoSuggestBoxSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			if (args.SelectedItem is string username)
			{
				_ = ViewModel.SelectCredential(username);
			}
		}

		private void AutoSuggestBoxLostFocus(object sender, RoutedEventArgs e)
		{
			var asb = (AutoSuggestBox)sender;
			_ = ViewModel.SelectCredential(asb.Text);
		}

		private void OnCredentialClicked(object sender, ItemClickEventArgs e)
		{
			var username = (string)e.ClickedItem;
			ViewModel.SelectCredential(username);
		}
		private void OnDeleteSalvedCredentialClicked(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var userName = (string)button.DataContext;
			ViewModel.DeleteCredential(userName);
		}
	}
}