using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace MyFTP.Utils.Converters
{
	public class ItemToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			// Check the property type, sometimes is IsEnabled (return a bool), sometimes is Visibility (return Windows.UI.Xaml.Visibility)
			// Using a converter, UIElement.Visibility disables the cast from bool to Windows.UI.Xaml.Visibility
			object trueValue;
			object falseValue;
			if (targetType == typeof(Visibility))
			{
				trueValue = Visibility.Visible;
				falseValue = Visibility.Collapsed;
			}
			else
			{
				trueValue = true;
				falseValue = false;
			}
			var type = value?.GetType();
			
			
			if (value is null)
			{
				return falseValue;
			}

			if (value is string s)
				return string.IsNullOrWhiteSpace(s) ? falseValue : trueValue;

			if (type.IsValueType)
			{
				var defaultValue = Activator.CreateInstance(type);
				return value.Equals(defaultValue) ? falseValue : trueValue;
			}
			return value == default ? falseValue : trueValue;
		}		

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
