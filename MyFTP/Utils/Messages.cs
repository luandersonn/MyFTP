using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using Windows.Storage;

namespace MyFTP.Utils
{
	/// <summary>
	/// Request open multiples files
	/// </summary>	
	public class RequestOpenFilesMessage : AsyncRequestMessage<IReadOnlyList<StorageFile>> { }
	/// <summary>
	/// Request to open a folder
	/// </summary>
	public class RequestOpenFolderMessage : AsyncRequestMessage<StorageFolder> { }
	/// <summary>
	/// Request to save content on file
	/// </summary>
	public class RequestSaveFileMessage : AsyncRequestMessage<StorageFile>
	{
		public string FileNameSuggestion { get; set; }
	}

	public class ErrorMessage
	{
		public ErrorMessage(Exception exception) => Exception = exception ?? throw new ArgumentNullException(nameof(exception));

		public Exception Exception { get; }
	}

	public class SelectedItemChangedMessage<T> : RequestMessage<T>
	{
		public SelectedItemChangedMessage() { }
		public SelectedItemChangedMessage(object sender, T item)
		{
			Sender = sender;
			Item = item;
		}

		public object Sender { get; }
		public T Item { get; }
	}
}
