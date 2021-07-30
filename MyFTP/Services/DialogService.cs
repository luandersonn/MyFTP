using Microsoft.Toolkit.Mvvm.Input;
using MyFTP.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MyFTP.Services
{
	public class DialogService : IDialogService
	{
		private readonly ISettings _settings;

		public DialogService(ISettings settings) => _settings = settings;

		public async Task<bool> AskForDeleteAsync(FtpListItemViewModel itemToDelete)
		{
			ElementTheme theme = default;
			_settings?.TryGet<ElementTheme>("AppTheme", out theme);

			var dialog = new Controls.DeleteItemDialog
			{
				DataContext = itemToDelete,
				RequestedTheme = theme
			};
			return await dialog.ShowAsync() == ContentDialogResult.Primary;
		}

		public async Task<bool> AskForReplaceAsync(StorageFile newFile, FtpListItemViewModel itemToReplace)
		{
			ElementTheme theme = default;
			_settings?.TryGet<ElementTheme>("AppTheme", out theme);

			var dialog = new Controls.ReplaceItemDialog(newFile, itemToReplace)
			{
				RequestedTheme = theme
			};
			return await dialog.ShowAsync() == ContentDialogResult.Primary;
		}

		public async Task OpenRenameDialogAsync(IAsyncRelayCommand<string> renameCommand, string originalName)
		{
			ElementTheme theme = default;
			_settings?.TryGet<ElementTheme>("AppTheme", out theme);

			var dialog = new Controls.RenameItemDialog(renameCommand, originalName)
			{
				RequestedTheme = theme
			};
			await dialog.ShowAsync();
		}
	}
}