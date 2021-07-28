using MyFTP.ViewModels;
using System;
using System.Threading.Tasks;

namespace MyFTP.Services
{
	public interface IDialogService
	{		
		Task<bool> AskForReplaceAsync(ViewModels.FtpListItemViewModel newItem, ViewModels.FtpListItemViewModel itemToReplace);
		Task<bool> AskForDeleteAsync(ViewModels.FtpListItemViewModel itemToDelete);
	}

	public class DialogService : IDialogService
	{
		public async Task<bool> AskForDeleteAsync(FtpListItemViewModel itemToDelete)
		{
			var dialog = new Controls.DeleteItemDialog
			{
				DataContext = itemToDelete
			};
			return await dialog.ShowAsync() == Windows.UI.Xaml.Controls.ContentDialogResult.Primary;
		}

		public Task<bool> AskForReplaceAsync(FtpListItemViewModel newItem, FtpListItemViewModel itemToReplace) => throw new System.NotImplementedException();
	}
}

