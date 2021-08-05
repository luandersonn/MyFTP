using System.Reflection;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace MyFTP.Utils
{
	public static class ControlExtensions
	{
		public static T GetTemplateChild<T>(this Control controlTemplate, string childName, bool isRequired = true)
			where T : DependencyObject
		{
			var methodInfo = controlTemplate.GetType().GetMethod(nameof(GetTemplateChild), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			T t = methodInfo.Invoke(controlTemplate, new[] { childName }) as T;
			return t;
		}

		public delegate void KeyboardAcceleratorInvokedCallBack(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args);
		public static KeyboardAccelerator AddKeyboardAccelerator(this UIElement element, VirtualKey Key, KeyboardAcceleratorInvokedCallBack callBack)
		{
			return AddKeyboardAccelerator(element, Key, VirtualKeyModifiers.None, callBack);
		}

		public static KeyboardAccelerator AddKeyboardAccelerator(this UIElement element, VirtualKey key, VirtualKeyModifiers modifiers, KeyboardAcceleratorInvokedCallBack callBack)
		{
			var accelerator = new KeyboardAccelerator
			{
				Key = key,
				Modifiers = modifiers
			};
			accelerator.Invoked += (s, e) => callBack(s, e);
			element.KeyboardAccelerators.Add(accelerator);
			return accelerator;
		}
	}
}
