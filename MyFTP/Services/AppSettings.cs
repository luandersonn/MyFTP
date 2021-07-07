using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

namespace MyFTP.Services
{
	public class AppSettings
	{

		#region properties
		public string AppVersion
		{
			get
			{
				var v = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.ApplicationVersion;
				return $"{v.Major}.{v.Minor}.{v.Revision}.{v.Build}";
			}
		}
		#endregion

		#region methods		

		/// <summary>
		/// Get a string from resources
		/// </summary>
		/// <param name="resourceName">The resource name</param>
		/// <param name="resource">The name of the file where the resource is located</param>
		/// <returns>The resource string or null if can't not find the resource</returns>
		public string GetStringFromResource(string resourceName, string resource = "Resources") => ResourceLoader.GetForViewIndependentUse(resource)?.GetString(resourceName);
		
		public bool TryGet<T>(string key, out T result, string containerKey = "default")
		{
			var container = GetContainer(containerKey);
			if (typeof(T).IsEnum)
			{
				if (TryGet<int>(key, out var int_value, containerKey))
				{
					result = (T)(object)int_value;
					return true;
				}
			}
			else if (container.Values[key] is T v)
			{
				result = v;
				return true;
			}
			result = default;
			return false;
		}
		public bool TrySet<T>(string key, T value, string containerKey = "default")
		{
			var container = GetContainer(containerKey);
			if (typeof(T).IsEnum)
			{
				return TrySet(key, (int)(object)value, containerKey);
			}
			else
			{
				if (!Equals(container.Values[key], value))
				{
					try
					{
						container.Values[key] = value;
						SettingChanged?.Invoke(this, new AppSettingChangedEventArgs(key, value, container));
						return true;
					}
					catch (Exception e) when (e.HResult == unchecked((int)0x8007065E))
					{
						container.Values[key] = value.ToString();
					}
					catch (Exception e)
					{
						Debug.WriteLine(e);
					}
				}
			}
			return false;
		}

		public bool TryGetList<T>(string key, out T[] result, string containerKey = "default")
		{
			var container = GetContainer(containerKey);

			if (typeof(T).IsEnum)
			{
				if (TryGetList<int>(key, out var int_values, containerKey))
				{
					result = int_values.Cast<T>().ToArray();
					return true;
				}
			}
			else if (container.Containers.ContainsKey(key))
			{
				try
				{
					result = container.Containers[key].Values.Values.Cast<T>().ToArray();
					return true;
				}
				catch { }
			}
			result = default;
			return false;
		}

		public bool TrySetList<T>(string key, IEnumerable<T> values, string containerKey = "default")
		{
			var container = GetContainer(containerKey);
			if (typeof(T).IsEnum)
			{
				return TrySetList(key, values.Cast<int>(), containerKey);
			}
			else
			{
				var listContainer = container.CreateContainer(key, ApplicationDataCreateDisposition.Always);
				try
				{
					listContainer.Values.Clear();
					int count = 0;
					foreach (var value in values)
					{
						listContainer.Values["item" + (count++)] = value;
					}
					SettingChanged?.Invoke(this, new AppSettingChangedEventArgs(key, values, container));
					return true;
				}
				catch (Exception e)
				{
					Debug.WriteLine(e);
				}
			}
			return false;
		}

		private ApplicationDataContainer GetContainer(string key)
		{
			return ApplicationData.Current.LocalSettings.CreateContainer(key, ApplicationDataCreateDisposition.Always);
		}
		#endregion

		public event EventHandler<AppSettingChangedEventArgs> SettingChanged;
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
