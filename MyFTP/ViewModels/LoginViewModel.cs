using FluentFTP;
using MyFTP.Services;
using MyFTP.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.System;

namespace MyFTP.ViewModels
{
	public class LoginViewModel : BindableItem
	{
		private ObservableCollection<string> _savedCredentialsList;
		private string _host;
		private int _port;
		private string _username;
		private string _password;
		private bool _saveCredentials;
		private AppSettings settings;

		private string _passwordCredentialResourceName = "MyFTP";

		public ReadOnlyObservableCollection<string> SavedCredentialsList { get; }
		public string Host { get => _host; set => Set(ref _host, value); }
		public int Port { get => _port; set => Set(ref _port, value); }
		public string Username { get => _username; set => Set(ref _username, value); }
		public string Password { get => _password; set => Set(ref _password, value); }
		public bool SaveCredentials { get => _saveCredentials; set => Set(ref _saveCredentials, value); }

		public LoginViewModel(DispatcherQueue dispatcher) : base(dispatcher)
		{
			_savedCredentialsList = new ObservableCollection<string>(GetAllCredentials().Select(x => x.UserName));
			SavedCredentialsList = new ReadOnlyObservableCollection<string>(_savedCredentialsList);

			// Get saved credentials
			settings = new AppSettings();
			if (settings.TryGet(nameof(Host), out _host)
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
			_saveCredentials = true;
		}

		public async Task<HostViewModel> ConnectAsync()
		{
			FtpClient client;
			// Anonymous login
			if (string.IsNullOrWhiteSpace(Username) && string.IsNullOrWhiteSpace(Password))
			{
				client = new FtpClient(Host);
				client.Port = Port;
				await client.ConnectAsync();
			}
			else
			{
				client = new FtpClient(Host, Port, new NetworkCredential(Username, Password));
				await client.ConnectAsync();
				if (SaveCredentials)
				{
					settings.TrySet(nameof(Host), Host);
					settings.TrySet(nameof(Port), Port);
					settings.TrySet(nameof(Username), Username);
					SaveCredentialsOnLocker(Username, Password);

				}
			}
			return new HostViewModel(client, DispatcherQueue.GetForCurrentThread());
		}

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
	}
}
