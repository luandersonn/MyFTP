using Microsoft.Toolkit.Mvvm.Input;
using MyFTP.Utils;
using MyFTP.Utils.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Store;
using Windows.System;

namespace MyFTP.ViewModels
{
	public class UpdateServiceViewModel : BindableItem
	{
		#region fields
		private SizeInBytesHumanizeConverter _byteConverter;
		private bool _isUpdateAvailable;
		private bool _isCheckingUpdates;
		private bool _isInstallingUpdates;
		private IReadOnlyList<StorePackageUpdate> _updates;
		private StorePackageUpdateStatus _updateInstallProgress;
		#endregion

		#region properties		
		public bool IsUpdateAvailable { get => _isUpdateAvailable; private set => Set(ref _isUpdateAvailable, value); }
		public IAsyncRelayCommand CheckForUpdatesCommand { get; }
		public IAsyncRelayCommand InstallUpdatesCommand { get; }
		public StorePackageUpdateStatus UpdateInstallProgress { get => _updateInstallProgress; private set => Set(ref _updateInstallProgress, value); }
		#endregion

		#region constructor		
		public UpdateServiceViewModel() : base(DispatcherQueue.GetForCurrentThread())
		{
			CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesCommandAsync, CanExecuteCheckForUpdates);
			InstallUpdatesCommand = new AsyncRelayCommand(InstallUpdateCommandAsync, CanExecuteInstallUpdate);

			_byteConverter = new SizeInBytesHumanizeConverter();
		}
		#endregion

		#region can execute
		private bool CanExecuteCheckForUpdates()
		{
			return !_isCheckingUpdates;
		}
		private bool CanExecuteInstallUpdate()
		{
			return !_isInstallingUpdates && _updates != null && _updates.Any();
		}
		#endregion

		#region methods
		public async Task CheckForUpdatesCommandAsync(CancellationToken token)
		{
			try
			{
				_isCheckingUpdates = true;
				CheckForUpdatesCommand.NotifyCanExecuteChanged();
				var context = StoreContext.GetDefault();
				_updates = await context.GetAppAndOptionalStorePackageUpdatesAsync().AsTask(token);
				IsUpdateAvailable = _updates.Any();
			}
			finally
			{
				_isCheckingUpdates = false;
				CheckForUpdatesCommand.NotifyCanExecuteChanged();
				InstallUpdatesCommand.NotifyCanExecuteChanged();
			}
		}

		private async Task InstallUpdateCommandAsync(CancellationToken token)
		{
			try
			{
				_isInstallingUpdates = true;
				InstallUpdatesCommand.NotifyCanExecuteChanged();
				var context = StoreContext.GetDefault();
				var progress = new Progress<StorePackageUpdateStatus>(OnInstallUpdateProgress);
				var result = await context.RequestDownloadAndInstallStorePackageUpdatesAsync(_updates).AsTask(token, progress);
				if (result.OverallState == StorePackageUpdateState.Completed)
				{
					_updates = null;
					IsUpdateAvailable = false;
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
			}
			finally
			{
				_isInstallingUpdates = false;
				UpdateInstallProgress = default;
				InstallUpdatesCommand.NotifyCanExecuteChanged();
			}
		}

		private void OnInstallUpdateProgress(StorePackageUpdateStatus progress)
		{
			UpdateInstallProgress = progress;
		}
		private async void OnEndUpdateInstallTask(Task<StorePackageUpdateResult> result) => await AccessUIAsync(() => UpdateInstallProgress = default);
		#endregion
	}
}
