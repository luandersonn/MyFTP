using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace MyFTP.Utils.Converters
{
	public class ItemVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var type = value?.GetType();

			if (value is null)
				return Visibility.Collapsed;

			if (value is string s)
				return string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible;

			if (type.IsValueType)
			{
				var defaultValue = Activator.CreateInstance(type);
				return value.Equals(defaultValue) ? Visibility.Collapsed : Visibility.Visible;
			}
			return value == default ? Visibility.Collapsed : Visibility.Visible;
		}
		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
