using FluentFTP;
using MyFTP.Services;
using MyFTP.Utils;
using System;
using System.Net;
using System.Threading.Tasks;
using Windows.System;

namespace MyFTP.ViewModels
{
	public class LoginViewModel : BindableItem
	{
		private string _host;
		private double _port;
		private string _username;
		private string _password;
		private bool _saveCredentials;
		private AppSettings settings;

		public string Host { get => _host; set => Set(ref _host, value); }
		public double Port { get => _port; set => Set(ref _port, value); }
		public string Username { get => _username; set => Set(ref _username, value); }
		public string Password { get => _password; set => Set(ref _password, value); }
		public bool SaveCredentials { get => _saveCredentials; set => Set(ref _saveCredentials, value); }

		public LoginViewModel(DispatcherQueue dispatcher) : base(dispatcher)
		{
			settings = new AppSettings();
			if (settings.TryGet(nameof(Host), out string result))
				Host = result;
			if (settings.TryGet(nameof(Port), out int port))
				Port = port;
			if (settings.TryGet(nameof(Username), out result))
				Username = result;
			_saveCredentials = true;
		}

		public async Task<HostViewModel> ConnectAsync()
		{
			var client = new FtpClient(Host, (int)Port, new NetworkCredential(Username, Password));
			await client.ConnectAsync();
			if (SaveCredentials)
			{
				settings.TrySet(nameof(Host), Host);
				settings.TrySet(nameof(Port), (int)Port);
				settings.TrySet(nameof(Username), Username);
			}

			return new HostViewModel(client, DispatcherQueue.GetForCurrentThread());
		}
	}
}
