using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyFTP.Utils
{
	public static class FileHelper
	{
		public static async Task<StorageFile> CreateTempFileAsync(string extension = "")
		{
			var folder = ApplicationData.Current.TemporaryFolder;
			return await folder.CreateFileAsync(Guid.NewGuid().ToString() + extension, CreationCollisionOption.GenerateUniqueName);
		}
	}
}
