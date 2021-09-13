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
		private static readonly string contentTypeKey = "System.ItemTypeText";
		private static StorageFolder Local => ApplicationData.Current.LocalFolder;

		public static async Task<StorageItemThumbnail> GetFileIconAsync(string fileExtension, uint size = 32)
		{
			var fileName = string.Format("dummy{0}", fileExtension);

			if (size == 32 && thumbnails.TryGetValue(fileName, out var thumbnail))
			{
				return thumbnail;
			}

			var folder = await Local.CreateFolderAsync(dummy_folder_name, CreationCollisionOption.OpenIfExists);
			var dummyFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
			thumbnail = await dummyFile.GetThumbnailAsync(ThumbnailMode.SingleItem, size, ThumbnailOptions.ResizeThumbnail);
			if (size == 32)
				thumbnails.TryAdd(fileName, thumbnail);
			return thumbnail;
		}

		public static async Task<StorageItemThumbnail> GetFolderIconAsync(uint size = 32)
		{
			var folderName = "dummyfolder";

			if (size == 32 && thumbnails.TryGetValue(folderName, out var thumbnail))
			{
				return thumbnail;
			}

			var folder = await Local.CreateFolderAsync(dummy_folder_name, CreationCollisionOption.OpenIfExists);
			var dummyFolder = await folder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
			thumbnail = await dummyFolder.GetThumbnailAsync(ThumbnailMode.SingleItem, size, ThumbnailOptions.ResizeThumbnail);
			if (size == 32)
				thumbnails.TryAdd(folderName, thumbnail);
			return thumbnail;
		}

		public static async Task<string> GetContentType(string fileExtension)
		{
			var fileName = string.Format("dummy{0}", fileExtension);
			var folder = await Local.CreateFolderAsync(dummy_folder_name, CreationCollisionOption.OpenIfExists);
			var dummyFile = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
			var properties = await dummyFile.Properties.RetrievePropertiesAsync(new string[] { contentTypeKey });
			if (properties.ContainsKey(contentTypeKey))
				return (string)properties[contentTypeKey];
			return dummyFile.ContentType;
		}
	}
}