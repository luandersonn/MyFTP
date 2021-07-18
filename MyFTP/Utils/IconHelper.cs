using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace MyFTP.Utils
{
	public static class IconHelper
	{
		private readonly static Dictionary<string, StorageItemThumbnail> thumbnails = new Dictionary<string, StorageItemThumbnail>();
		private static readonly string dummy_folder_name = "dummy";
		private static StorageFolder Local => ApplicationData.Current.LocalFolder;

		public static async Task<StorageItemThumbnail> GetFileIconAsync(string fileExtension)
		{
			var fileName = string.Format("dummy{0}", fileExtension);

			if (thumbnails.TryGetValue(fileName, out var thumbnail))
			{
				return thumbnail;
			}

			var folder = await Local.CreateFolderAsync(dummy_folder_name, CreationCollisionOption.OpenIfExists);


			var dummyFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
			thumbnail = await dummyFile.GetThumbnailAsync(ThumbnailMode.SingleItem, 32, ThumbnailOptions.ResizeThumbnail);
			thumbnails.TryAdd(fileName, thumbnail);
			return thumbnail;
		}

		public static async Task<StorageItemThumbnail> GetFolderIconAsync()
		{
			var folderName = "dummyfolder";

			if (thumbnails.TryGetValue(folderName, out var thumbnail))
			{
				return thumbnail;
			}

			var folder = await Local.CreateFolderAsync(dummy_folder_name, CreationCollisionOption.OpenIfExists);
			var dummyFolder = await folder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
			thumbnail = await dummyFolder.GetThumbnailAsync(ThumbnailMode.SingleItem, 32, ThumbnailOptions.ResizeThumbnail);
			thumbnails.TryAdd(folderName, thumbnail);
			return thumbnail;
		}
	}
}
