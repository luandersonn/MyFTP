using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.ViewModels;
using System;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;

namespace MyFTP.Controls
{
	public sealed partial class LoginDialog : ContentDialog
	{
		public LoginDialog()
		{
			this.InitializeComponent();
			DataContext = App.Current.Services.GetRequiredService<LoginViewModel>();
			Opened += (sender, args) => WeakReferenceMessenger.Default.Register<FtpListItemViewModel>(this, SuccessfulLogin);
			Closed += (sender, args) => WeakReferenceMessenger.Default.Unregister<FtpListItemViewModel>(this);
		}
		public LoginViewModel ViewModel => (LoginViewModel)DataContext;
		private void SuccessfulLogin(object recipient, FtpListItemViewModel message)
		{
			Result = message;
		}

		public FtpListItemViewModel Result { get; private set; }

		private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			var defer = args.GetDeferral();
			try
			{
				IsEnabled = false;
				await ViewModel.LoginCommand.ExecuteAsync(null);
				
			}
			catch(Exception e)
			{
				Debug.WriteLine(e);
				args.Cancel = true;
			}
			finally
			{
				IsEnabled = true;
				defer.Complete();
			}
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
		}
	}
}
