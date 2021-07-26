using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System.Collections.Generic;
using Windows.Storage;

namespace MyFTP.Utils
{
	public class RequestOpenFilesMessage : AsyncRequestMessage<IReadOnlyList<StorageFile>>
	{
	}
	public class RequestOpenFolderMessage : AsyncRequestMessage<StorageFolder>
	{
	}
	public class RequestSaveFileMessage : AsyncRequestMessage<StorageFile>
	{		
		public string FileNameSuggestion { get; set; }
	}
	public class RequestSaveFolderMessage : AsyncRequestMessage<StorageFolder>
	{
	}
}