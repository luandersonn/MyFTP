using System.Threading.Tasks;
using Windows.Storage;

namespace MyFTP.Services
{
	public interface IDialogService
	{
		Task<bool> AskForReplaceAsync(StorageFile newFile, ViewModels.FtpListItemViewModel itemToReplace);
		Task<bool> AskForDeleteAsync(ViewModels.FtpListItemViewModel itemToDelete);
	}
}

