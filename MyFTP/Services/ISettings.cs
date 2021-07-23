using System;
using System.Collections.Generic;
using Windows.Storage;

namespace MyFTP.Services
{
	public interface ISettings
	{
		string AppVersion { get; }
		string GetStringFromResource(string resourceName, string resource = "Resources");
		bool TryGet<T>(string key, out T result, string containerKey = "default");
		bool TrySet<T>(string key, T value, string containerKey = "default");
		bool TryGetList<T>(string key, out T[] result, string containerKey = "default");
		bool TrySetList<T>(string key, IEnumerable<T> values, string containerKey = "default");
		event EventHandler<AppSettingChangedEventArgs> SettingChanged;
	}

	public class AppSettingChangedEventArgs : EventArgs
	{
		public AppSettingChangedEventArgs(string key, object value, ApplicationDataContainer container)
		{
			Key = key;
			Value = value;
			Container = container;
		}

		public object Value { get; }
		public string Key { get; }
		public ApplicationDataContainer Container { get; }
	}
}
