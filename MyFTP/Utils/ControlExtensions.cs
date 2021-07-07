using System;
using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MyFTP.Utils
{
	public static class ControlExtensions
	{
		public static T GetTemplateChild<T>(this Control controlTemplate, string childName, bool isRequired = true)
			where T : DependencyObject
		{
			var methodInfo = controlTemplate.GetType().GetMethod(nameof(GetTemplateChild), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			T t = methodInfo.Invoke(controlTemplate, new[] { childName }) as T;

			if (isRequired & t is null)
			{
				throw new NullReferenceException();
			}

			return t;
		}
	}
}
