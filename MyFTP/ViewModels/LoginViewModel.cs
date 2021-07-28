using FluentFTP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.Services;
using MyFTP.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace MyFTP.ViewModels
{
	public class LoginViewModel : BindableItem
	{
		#region fields		
		private ObservableCollection<string> _savedCredentialsList;
		private string _host;
		private int _port;
		private string _username;
		private string _password;
		private bool _saveCredentials;
		private readonly ISettings settings;
		private bool _isLoggingin;
		private string _passwordCredentialResourceName = "MyFTP";
		#endregion

		#region properties		
		public ReadOnlyObservableCollection<string> SavedCredentialsList { get; }
		public string Host { get => _host; set => Set(ref _host, value); }
		public int Port { get => _port; set => Set(ref _port, value); }
		public string Username { get => _username; set => Set(ref _username, value); }
		public string Password { get => _password; set => Set(ref _password, value); }
		public bool SaveCredentials { get => _saveCredentials; set => Set(ref _saveCredentials, value); }
		public bool CanSaveCredentials => settings != null;
		public IAsyncRelayCommand LoginCommand { get; }
		#endregion

		#region constructor		
		public LoginViewModel(ISettings settings)
		{
			this.settings = settings;

			_savedCredentialsList = new ObservableCollection<string>(GetAllCredentials().Select(x => x.UserName));
			SavedCredentialsList = new ReadOnlyObservableCollection<string>(_savedCredentialsList);

			// Get saved credentials
			if (settings != null
				&& settings.TryGet(nameof(Host), out _host)
				&& settings.TryGet(nameof(Port), out _port)
				&& settings.TryGet(nameof(Username), out _username))
			{
				var credential = GetCredentialFromLocker(_username);
				if (credential != null)
				{
					credential.RetrievePassword();
					_password = credential.Password;
				}
			}
			_saveCredentials = settings != null;

			LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
		}
		#endregion

		#region methods
		public bool SelectCredential(string username)
		{
			var credential = GetCredentialFromLocker(username);
			if (credential != null)
			{
				credential.RetrievePassword();
				Username = username;
				Password = credential.Password;
				return true;
			}
			return false;
		}
		private bool CanLogin() => !_isLoggingin;
		private async Task LoginAsync(CancellationToken token)
		{
			IFtpClient client;
			_isLoggingin = true;
			LoginCommand.NotifyCanExecuteChanged();
			// Anonymous login
			try
			{

				if (string.IsNullOrWhiteSpace(Username) && string.IsNullOrWhiteSpace(Password))
				{
					client = new FtpClient(Host);
					client.Port = Port;
					await client.ConnectAsync(token);
				}
				else
				{
					client = new FtpClient(Host, Port, new NetworkCredential(Username, Password));
					await client.ConnectAsync(token);
					if (SaveCredentials && settings != null)
					{
						settings.TrySet(nameof(Host), Host);
						settings.TrySet(nameof(Port), Port);
						settings.TrySet(nameof(Username), Username);
						SaveCredentialsOnLocker(Username, Password);
					}
				}
				var transferService = App.Current.Services.GetService<ITransferItemService>();
				var dialogService = App.Current.Services.GetService<IDialogService>();
				var hostVM = new HostViewModel(client, transferService, dialogService);
				WeakReferenceMessenger.Default.Send<HostViewModel>(hostVM);
			}
			finally
			{
				_isLoggingin = false;
				LoginCommand.NotifyCanExecuteChanged();
			}
		}

		private void SaveCredentialsOnLocker(string username, string password)
		{
			var vault = new PasswordVault();
			vault.Add(new PasswordCredential(_passwordCredentialResourceName, username, password));
		}

		private PasswordCredential GetCredentialFromLocker(string username)
		{
			try
			{
				var vault = new PasswordVault();
				return vault.Retrieve(_passwordCredentialResourceName, username);
			}
			catch
			{
				return null;
			}
		}

		private IEnumerable<PasswordCredential> GetAllCredentials()
		{
			try
			{
				PasswordVault vault = new PasswordVault();
				return vault.FindAllByResource(_passwordCredentialResourceName);
			}
			catch
			{
				return new PasswordCredential[] { };
			}
		}
		#endregion
	}
}
