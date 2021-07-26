using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System.Collections.Generic;
using Windows.Storage;

namespace MyFTP.Utils
{
	public class RequestFileMessage : AsyncRequestMessage<IReadOnlyList<StorageFile>>
	{

	}
}