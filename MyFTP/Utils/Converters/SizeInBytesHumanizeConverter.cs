using Humanizer;
using System;
using Windows.UI.Xaml.Data;

namespace MyFTP.Utils.Converters
{
	public class SizeInBytesHumanizeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is long bytes)
			{
				if (bytes < 0)
					return "";
				return bytes.Bytes().ToString("#.##");
			}
			return value.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}
}
