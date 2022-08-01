using FluentFTP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.Services;
using MyFTP.Utils;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;

namespace MyFTP.ViewModels
{
	public class LoginViewModel : BindableItem
	{
		#region fields		
		private ObservableCollection<FtpHostSettingsViewModel> _savedCredentialsList;
		private string _host;
		private int _port;
		private string _username;
		private string _password;
		private bool _saveCredentials;
		private readonly ISettings settings;
		private bool _isBusy;
		#endregion

		#region properties		
		public ReadOnlyObservableCollection<FtpHostSettingsViewModel> SavedCredentialsList { get; }
		public string Host { get => _host; set => Set(ref _host, value); }
		public int Port { get => _port; set => Set(ref _port, value); }
		public string Username { get => _username; set => Set(ref _username, value); }
		public string Password { get => _password; set => Set(ref _password, value); }
		public bool SaveCredentials { get => _saveCredentials; set => Set(ref _saveCredentials, value); }
		public bool CanSaveCredentials => settings != null;
		public IAsyncRelayCommand LoginCommand { get; }
		#endregion

		#region constructor		
		public LoginViewModel(ISettings settings) : base(DispatcherQueue.GetForCurrentThread())
		{
			this.settings = settings;

			_savedCredentialsList = new ObservableCollection<FtpHostSettingsViewModel>();
			SavedCredentialsList = new ReadOnlyObservableCollection<FtpHostSettingsViewModel>(_savedCredentialsList);

			_saveCredentials = true;
			Port = 21;

			LoginCommand = new AsyncRelayCommand<FtpHostSettingsViewModel>(LoginCommandAsync, CanLogin);

			Task.Run(LoadSavedHostsAsync);
		}
		#endregion

		#region methods
		private bool CanLogin(FtpHostSettingsViewModel args) => !_isBusy;

		private async Task LoginCommandAsync(FtpHostSettingsViewModel arg, CancellationToken token)
		{
			ILogger logger = null;
			FtpClient client = null;
			string host = null;
			string username = null;
			string password = null;
			int port = 0;

			try
			{
				_isBusy = true;
				LoginCommand.NotifyCanExecuteChanged();

				if (arg == null)
				{
					host = Host;
					username = Username;
					password = Password;
					port = Port;
				}
				else
				{
					host = arg.Host;
					username = arg.Username;

					var credential = arg.Item.GetCredentialFromLocker();
					if (credential != null)
					{
						credential.RetrievePassword();
						password = credential.Password;
					}
					port = arg.Port;
				}


				if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password))
				{
					// Anonymous login
					client = new FtpClient(host);
					client.Port = Port;
				}
				else
				{
					client = new FtpClient(host, port, new NetworkCredential(username, password));
				}

				logger = LoggerFactory.CreateLogger($"{host}:{port}:{username}");
				if (logger != null)
				{
					client.OnLogEvent += (s, e) => logger.WriteLine(e);
				}

				await client.ConnectAsync(token);

				if (SaveCredentials && arg == null)
				{
					var ftpHostSettings = await FtpHostSettings.GetOrCreateAsync(host, port, username);
					if (!string.IsNullOrWhiteSpace(password))
						ftpHostSettings.SavePasswordOnLocker(password);
				}

				// Load root item
				var root = await client.GetObjectInfoAsync("/");

				if (root is null)
				{
					var d = default(DateTime);
					root = new FtpListItem("", "/", -1, true, d)
					{
						FullName = "/",
						Type = FtpObjectType.Directory
					};
				}
				var transferService = App.Current.Services.GetService<ITransferItemService>();
				var dialogService = App.Current.Services.GetService<IDialogService>();

				// Create FTPItemViewModel
				var rootViewModel = new FtpListItemViewModel(client, root, null, transferService, dialogService, logger);

				// Send to view
				WeakReferenceMessenger.Default.Send<FtpListItemViewModel>(rootViewModel);
			}
			catch (Exception ex)
			{
				logger?.WriteLine(ex.ToString());
				logger?.Dispose();
				throw;
			}
			finally
			{
				_isBusy = false;
				LoginCommand.NotifyCanExecuteChanged();
			}
		}

		public void Delete(FtpHostSettingsViewModel item)
		{
			_savedCredentialsList.Remove(item);
		}

		private async Task LoadSavedHostsAsync()
		{
			var hosts = await FtpHostSettings.GetAllAsync();
			await AccessUIAsync(() => _savedCredentialsList.Clear());
			foreach (var (_, host) in hosts)
			{
				await AccessUIAsync(() => _savedCredentialsList.Add(new FtpHostSettingsViewModel(host)));
			}
		}
		#endregion
	}
}