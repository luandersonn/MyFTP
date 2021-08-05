using Humanizer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace MyFTP.Services
{
	public class StoreService
	{
		public StoreContext Context { get; private set; }

		#region rate and review
		public async Task<StoreRateAndReviewResult> RequestRateAndReviewAsync()
		{
			if (Context == null) StoreContext.GetDefault();
			return await Context.RequestRateAndReviewAppAsync();
		}
		#endregion

		#region updates		
		public async Task<IReadOnlyList<StorePackageUpdate>> GetAvaiableUpdatesAsync()
		{
			if (Context == null) StoreContext.GetDefault();
			return await Context.GetAppAndOptionalStorePackageUpdatesAsync();
		}

		public async Task<StorePackageUpdateResult> DownloadUpdateAsync(IProgress<StorePackageUpdateStatus> progress, CancellationToken cancellationToken)
		{
			if (Context == null) StoreContext.GetDefault();
			var updates = await Context.GetAppAndOptionalStorePackageUpdatesAsync();
			return updates.Count > 0
				? await Context.RequestDownloadAndInstallStorePackageUpdatesAsync(updates).AsTask(cancellationToken, progress)
				: null;
		}

		public async Task<StorePackageUpdateResult> RequestDownloadAndInstallStorePackageUpdatesAsync(IEnumerable<StorePackageUpdate> packages,
																								IProgress<StorePackageUpdateStatus> progress,
																								CancellationToken cancellationToken)
		{
			if (Context == null) StoreContext.GetDefault();
			return await Context.RequestDownloadAndInstallStorePackageUpdatesAsync(packages).AsTask(cancellationToken, progress);
		}

		public bool CanSilentlyDownloadStorePackageUpdates
		{
			get
			{
				if (Context == null)
					Context = StoreContext.GetDefault();
				return Context.CanSilentlyDownloadStorePackageUpdates;
			}
		}

		public async Task<bool> TryDownloadUpdateInBackgroundAsync(CancellationToken token)
		{
			if (Context == null) StoreContext.GetDefault();
			var canDownload = Context.CanSilentlyDownloadStorePackageUpdates;
			if (canDownload)
			{
				// Get the updates that are available.
				var storePackageUpdates = await Context.GetAppAndOptionalStorePackageUpdatesAsync();
				if (storePackageUpdates.Count > 0)
				{
					// Start the silent downloads and wait for the downloads to complete.
					var downloadResult = await Context.TrySilentDownloadStorePackageUpdatesAsync(storePackageUpdates);
					switch (downloadResult.OverallState)
					{
						case StorePackageUpdateState.Completed:
							// The download has completed successfully.							
							await InstallUpdateAsync(storePackageUpdates);
							return true;

						default:
							return false;
					}
				}
				else
				{
					return false;
				}
			}
			else
				return false;
		}

		private async Task InstallUpdateAsync(IReadOnlyList<StorePackageUpdate> storePackageUpdates)
		{
			// Start the silent installation of the packages. Because the packages have already
			// been downloaded in the previous method, the following line of code just installs
			// the downloaded packages.			
			var downloadResult = await Context.TrySilentDownloadAndInstallStorePackageUpdatesAsync(storePackageUpdates);
		}

		private void DownloadProgress(StorePackageUpdateStatus spus)
		{
			var status = spus.PackageUpdateState.Humanize(LetterCasing.Sentence);
		}

		private void EndOperation(Task<StorePackageUpdateResult> state)
		{
			var status = state.Result.OverallState.Humanize(LetterCasing.Sentence);
		}
		#endregion

		#region IAP
		public async Task<IEnumerable<StoreProduct>> GetStoreProductsAsync()
		{
			if (Context == null) StoreContext.GetDefault();
			var requestResult = await Context.GetAssociatedStoreProductsAsync(new string[] { "Durable", "Consumable", "UnmanagedConsumable" });
			return requestResult.ExtendedError != null ? throw requestResult.ExtendedError : requestResult.Products.Values;
		}

		public async Task<StorePurchaseStatus> RequestPurchaseAsync(string storeId)
		{
			if (Context == null) StoreContext.GetDefault();
			var requestResult = await Context.RequestPurchaseAsync(storeId);
			return requestResult.ExtendedError != null ? throw requestResult.ExtendedError : requestResult.Status;
		}
		#endregion

	}
}