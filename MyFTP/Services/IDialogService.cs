using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyFTP.Services
{
	public interface IDialogService
	{
		Task<bool> AskForReplaceAsync(StorageFile newFile, ViewModels.FtpListItemViewModel itemToReplace);
		Task<bool> AskForDeleteAsync(IEnumerable<ViewModels.FtpListItemViewModel> itemsToDelete);
		Task OpenRenameDialogAsync(IAsyncRelayCommand<string> renameCommand, string originalName);
	}
}

