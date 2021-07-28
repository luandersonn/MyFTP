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

			thumbnail = await GetFileIconAsync(fileExtension, 32);
			thumbnails.TryAdd(fileName, thumbnail);
			return thumbnail;
		}
		public static async Task<StorageItemThumbnail> GetFileIconAsync(string fileExtension, uint size)
		{
			var fileName = string.Format("dummy{0}", fileExtension);
			var folder = await Local.CreateFolderAsync(dummy_folder_name, CreationCollisionOption.OpenIfExists);
			var dummyFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
			var thumbnail = await dummyFile.GetThumbnailAsync(ThumbnailMode.SingleItem, size, ThumbnailOptions.ResizeThumbnail);
			return thumbnail;
		}

		public static async Task<StorageItemThumbnail> GetFolderIconAsync()
		{
			var folderName = "dummyfolder";

			if (thumbnails.TryGetValue(folderName, out var thumbnail))
			{
				return thumbnail;
			}

			thumbnail = await GetFolderIconAsync(32);
			thumbnails.TryAdd(folderName, thumbnail);
			return thumbnail;
		}

		public static async Task<StorageItemThumbnail> GetFolderIconAsync(uint size)
		{
			var folderName = "dummyfolder";
			var folder = await Local.CreateFolderAsync(dummy_folder_name, CreationCollisionOption.OpenIfExists);
			var dummyFolder = await folder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
			var thumbnail = await dummyFolder.GetThumbnailAsync(ThumbnailMode.SingleItem, size, ThumbnailOptions.ResizeThumbnail);
			return thumbnail;
		}
	}
}