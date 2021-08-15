using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace MyFTP.Utils
{
	public class FtpHostSettings
	{
		private static readonly string _passwordCredentialResourceName = "MyFTP";
		private static readonly string FileName = "servers.json";		
		public string Id { get; set; }
		public string Host { get; set; }
		public string Username { get; set; }
		public int Port { get; set; }

		public async Task SaveAsync()
		{
			var dic = await GetAllAsync();
			dic[Id] = this;
			var json = JsonConvert.SerializeObject(dic, Formatting.Indented);
			var folder = ApplicationData.Current.LocalFolder;
			var localFile = await folder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
			await FileIO.WriteTextAsync(localFile, json);
		}

		public async Task<StorageFolder> GetDefaultSaveLocationAsync()
		{
			try
			{
				return await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(Id, AccessCacheOptions.SuppressAccessTimeUpdate);
			}
			catch
			{
				return ApplicationData.Current.TemporaryFolder;
			}
		}

		public void SetDefaultSaveLocation(StorageFolder location)
		{
			StorageApplicationPermissions.FutureAccessList.AddOrReplace(Id, location);
		}		

		public static async Task DeleteAsync(string id)
		{
			var all = await GetAllAsync();
			all.Remove(id);
			var json = JsonConvert.SerializeObject(all, Formatting.Indented);
			var folder = ApplicationData.Current.LocalFolder;
			var localFile = await folder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
			await FileIO.WriteTextAsync(localFile, json);
		}

		public static async Task<IDictionary<string, FtpHostSettings>> GetAllAsync()
		{
			StorageFile localFile = null;
			string json = null;
			try
			{
				var folder = ApplicationData.Current.LocalFolder;
				localFile = await folder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
				json = await FileIO.ReadTextAsync(localFile);
			}
			catch (Exception e)
			{
				Debug.WriteLine("Error on read servers.json file: " + e);
				return new Dictionary<string, FtpHostSettings>();
			}
			return JsonConvert.DeserializeObject<Dictionary<string, FtpHostSettings>>(json) ?? new Dictionary<string, FtpHostSettings>();
		}		

		#region locker
		public PasswordCredential GetCredentialFromLocker()
		{
			try
			{
				var vault = new PasswordVault();
				return vault.Retrieve(_passwordCredentialResourceName, Id);
			}
			catch
			{
				return null;
			}
		}
		private void SaveCredentialsOnLocker(string username, string password)
		{
			var vault = new PasswordVault();
			vault.Add(new PasswordCredential(_passwordCredentialResourceName, username, password));
		}

		private void RemoveCredentialFromLocker(PasswordCredential credential)
		{
			var vault = new PasswordVault();
			vault.Remove(credential);
		}
		#endregion

		public override string ToString() => $"{Username}@{Host}@{Port}";
	}
}