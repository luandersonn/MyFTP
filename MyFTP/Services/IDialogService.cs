using System.Threading.Tasks;

namespace MyFTP.Services
{
	public interface IDialogService
	{
		Task ShowMessageAsync(string title, string content);
		Task<string> ShowRenameDialogAsync();
		Task<bool> ShowDeleteDialogAsync(string remotePath);
	}
}
