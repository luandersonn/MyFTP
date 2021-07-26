using Humanizer;
using System;
using Windows.UI.Xaml.Data;

namespace MyFTP.Utils.Converters
{
	public class DateTimeHumanizeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is DateTime date && date != default)
			{
				return date.Humanize();
			}
			return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}
}
