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
		private static readonly string _passwordCredentialResourceName = "MyFTPClient";
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

		public static async Task<FtpHostSettings> GetAsync(string host, int port, string username)
		{
			var id = GetId(host, port, username);
			return await GetAsync(id);
		}

		public static async Task<FtpHostSettings> GetAsync(string id)
		{
			var dic = await GetAllAsync();
			if (dic.ContainsKey(id))
				return dic[id];
			return null;
		}

		public static async Task<FtpHostSettings> GetOrCreateAsync(string host, int port, string username)
		{
			var dic = await GetAllAsync();
			var id = GetId(host, port, username);
			if (dic.ContainsKey(id))
			{
				return dic[id];
			}
			var settings = new FtpHostSettings
			{
				Id = id,
				Host = host,
				Port = port,
				Username = username
			};
			await settings.SaveAsync();
			return settings;
		}

		public static async Task DeleteAsync(string id)
		{			
			var all = await GetAllAsync();
			if (all.ContainsKey(id))
			{
				all[id].DeleteCredentialFromLocker();
				all.Remove(id);
			}
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
			ClearOldCredentials();
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
		public void SavePasswordOnLocker(string password)
		{
			var vault = new PasswordVault();			
			vault.Add(new PasswordCredential(_passwordCredentialResourceName, Id, password));
		}

		public void DeleteCredentialFromLocker()
		{
			var credential = GetCredentialFromLocker();
			if (credential != null)
			{
				var vault = new PasswordVault();
				vault.Remove(credential);
			}
		}

		private void RemoveCredentialFromLocker(PasswordCredential credential)
		{
			var vault = new PasswordVault();
			vault.Remove(credential);
		}

		// Clear old passwords
		private static void ClearOldCredentials()
		{
			var vault = new PasswordVault();

			var oldResourceName = "MyFTP";
			try
			{
				foreach (var item in vault.FindAllByResource(oldResourceName))
				{
					vault.Remove(item);
				}
			}
			catch(Exception e)
			{
				Debug.WriteLine(e);
			}
		}
		#endregion

		public static string GetId(string host, int port, string username) => $"{username}@{host}:{port}";

		public override string ToString() => $"{Username}@{Host}:{Port}";
	}
}