using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Collections.Generic;

namespace MyFTP.Services
{
	public class AppCenterService
	{
		private readonly ISettings _settings;

		public AppCenterService(ISettings settings) => _settings = settings;

		public void TrackEvent(string title, params (string, object)[] values)
		{
			var props = GetAppInfo();
			foreach (var (key, value) in values)
			{
				if (key != null)
					props.TryAdd(key, value?.ToString() ?? "{null}");
			}
			Analytics.TrackEvent(title, props);
		}


		private Dictionary<string, string> GetAppInfo()
		{
			return new Dictionary<string, string>
			{
				{ "App Version", _settings?.AppVersion },
				{ "Device Family", SystemInformation.Instance.DeviceFamily },
				{ "Device Manufacturer", SystemInformation.Instance.DeviceManufacturer },
				{ "Device Model", SystemInformation.Instance.DeviceModel },
				{ "Operating System", SystemInformation.Instance.OperatingSystem },
				{ "Operating System Architecture", SystemInformation.Instance.OperatingSystemArchitecture.ToString() },
				{ "Operating System Version", SystemInformation.Instance.OperatingSystemVersion.ToString() }
			};
		}
	}
}
