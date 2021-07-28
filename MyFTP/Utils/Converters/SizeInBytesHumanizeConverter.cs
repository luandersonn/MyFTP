using Humanizer;
using System;
using Windows.UI.Xaml.Data;

namespace MyFTP.Utils.Converters
{
	public class SizeInBytesHumanizeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			try
			{
				if (value is ulong l)
				{
					if (l > long.MaxValue)
						return ConvertUlong(l);
					else
						value = long.Parse(l.ToString());
				}

				var bytes = (long)value;
				if (bytes < 0)
					return "";
				return bytes.Bytes().ToString("#.##");
			}
			catch
			{
				return value.ToString();
			}
		}

		private string ConvertUlong(ulong bytes)
		{
			string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
			int order = 0;
			while (bytes >= 1024 && order < sizes.Length - 1)
			{
				order++;
				bytes = bytes / 1024;
			}
			return string.Format("{0:0.##} {1}", bytes, sizes[order]);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}
}
