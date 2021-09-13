using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace MyFTP.Utils
{
	public static class WindowHelper
	{
		public static bool IsAppWindowSupported => ApiInformation.IsTypePresent("Windows.UI.WindowManagement.AppWindow");

		public static async Task<AppWindow> OpenInNewAppWindowAsync<T>(
					object parameter,
					AppWindowPresentationKind presentationKind = AppWindowPresentationKind.Default,
					string title = null)
			where T : Page
		{
			if (!IsAppWindowSupported)
				throw new NotSupportedException();

			// Create the AppWindow
			AppWindow appWindow = await AppWindow.TryCreateAsync();

			// Create the main frame and navigate for the page
			var appWindowContentFrame = new Frame();
			appWindowContentFrame.Navigate(typeof(T), parameter);

			// Set the AppWindow content
			ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowContentFrame);

			// Resize
			var windowSize = new Size(500, 360);
			appWindow.RequestSize(windowSize);

			// Placement
			var windowPlacement = CalculateWindowPlacement(windowSize);
			appWindow.RequestMoveRelativeToCurrentViewContent(windowPlacement);

			// Set title
			if (title != null)
				appWindow.Title = title;

			// Set the presentation mode
			var result = appWindow.Presenter.RequestPresentation(presentationKind);

			// Try show the app Window
			result = await appWindow.TryShowAsync();

			if (result)
			{
				// Handle the close window event
				appWindow.Closed += (s, e) =>
				{
					appWindowContentFrame.Content = null;
					appWindow = null;
				};
				// return the result
				return appWindow;
			}
			return null;
		}

		/// <summary>
		/// Calculate the AppWindow offset based on current window
		/// </summary>
		/// <param name="appWindowSize"></param>
		/// <returns></returns>
		private static Point CalculateWindowPlacement(Size appWindowSize)
		{
			var windowWidth = Window.Current.Bounds.Width;
			var windowHeight = Window.Current.Bounds.Height;
			var x = (windowWidth / 2) - (appWindowSize.Width / 2);
			var y = (windowHeight / 2) - (appWindowSize.Height / 2);
			return new Point(x, y);
		}
	}
}
