using MyFTP.Utils;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace MyFTP.Controls
{
	[ContentProperty(Name = "Content")]
	public sealed partial class TitleBarControl : Control
	{
		#region constructor		
		public TitleBarControl()
		{
			DefaultStyleKey = typeof(TitleBarControl);
			Loaded += (sender, args) =>
			{
				// Register a handler for when the title bar visibility changes.
				// For example, when the title bar is invoked in full screen mode.
				//GetCoreTitleBar().IsVisibleChanged += CoreTitleBarIsVisibleChanged;

				// Register a handler for when the size of the overlaid caption control changes.
				// For example, when the app moves to a screen with a different DPI.
				GetCoreTitleBar().LayoutMetricsChanged += CoreTitleBarLayoutMetricsChanged;

				ApplyLayout();

				Window.Current.Activated += WindowActivated;

				ActualThemeChanged += OnActualThemeChanged;

				ConfigureTitleBarTheme(ActualTheme);
				SetWindowTitle(Title);
			};

			Unloaded += (sender, args) =>
			{
				//GetCoreTitleBar().IsVisibleChanged -= CoreTitleBarIsVisibleChanged;
				GetCoreTitleBar().LayoutMetricsChanged -= CoreTitleBarLayoutMetricsChanged;
				ActualThemeChanged -= OnActualThemeChanged;
				Window.Current.Activated -= WindowActivated;
			};
		}
		#endregion

		#region private properties		
		private UIElement AppTitleBar { get; set; }
		private ContentPresenter ContentPresenter { get; set; }
		private ContentPresenter InnerContentPresenter { get; set; }
		private ColumnDefinition LeftPaddingColumn { get; set; }
		private ColumnDefinition RightPaddingColumn { get; set; }
		#endregion

		#region public properties
		public object Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }
		public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(object), typeof(TitleBarControl), new PropertyMetadata(null, (sender, args) =>
		{
			if (sender is TitleBarControl titleBarControl)
			{
				titleBarControl.ContentVisibility = args.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}));

		public Visibility ContentVisibility { get => (Visibility)GetValue(ContentVisibilityProperty); set => SetValue(ContentVisibilityProperty, value); }
		public static readonly DependencyProperty ContentVisibilityProperty = DependencyProperty.Register(nameof(ContentVisibility), typeof(Visibility), typeof(TitleBarControl), new PropertyMetadata(Visibility.Collapsed));


		public object InnerContent { get => GetValue(InnerContentProperty); set => SetValue(InnerContentProperty, value); }
		public static readonly DependencyProperty InnerContentProperty = DependencyProperty.Register(nameof(InnerContent), typeof(object), typeof(TitleBarControl), new PropertyMetadata(null, (sender, args) =>
		{
			if (sender is TitleBarControl titleBarControl)
			{
				titleBarControl.InnerContentVisibility = args.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}));

		public Visibility InnerContentVisibility { get => (Visibility)GetValue(InnerContentVisibilityProperty); set => SetValue(InnerContentVisibilityProperty, value); }
		public static readonly DependencyProperty InnerContentVisibilityProperty = DependencyProperty.Register(nameof(InnerContentVisibility), typeof(Visibility), typeof(TitleBarControl), new PropertyMetadata(Visibility.Collapsed));


		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(TitleBarControl), new PropertyMetadata(null, (sender, args) =>
		{
			if (sender is TitleBarControl titleBarControl)
			{
				titleBarControl.SetWindowTitle(args.NewValue as string);
			}
		}));


		#endregion

		#region private methods
		private CoreApplicationViewTitleBar GetCoreTitleBar() => CoreApplication.GetCurrentView().TitleBar;
		private ApplicationViewTitleBar GetAppViewTitleBar() => ApplicationView.GetForCurrentView().TitleBar;

		private void CoreTitleBarLayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args) => UpdateTitleBarLayout(sender);

		//private void CoreTitleBarIsVisibleChanged(CoreApplicationViewTitleBar sender, object args) => Visibility = sender.IsVisible ? Visibility.Visible : Visibility.Collapsed;

		private void OnActualThemeChanged(FrameworkElement sender, object args) => ConfigureTitleBarTheme(ActualTheme);

		private void WindowActivated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
		{
			switch (e.WindowActivationState)
			{
				// Setting the title bar to 40% opacity makes it easy for the user to identify that the window is inactive
				case Windows.UI.Core.CoreWindowActivationState.Deactivated:
					ContentPresenter.Opacity = InnerContentPresenter.Opacity = 0.4;
					break;

				// Back to active state
				case Windows.UI.Core.CoreWindowActivationState.CodeActivated:
				case Windows.UI.Core.CoreWindowActivationState.PointerActivated:
					ContentPresenter.Opacity = InnerContentPresenter.Opacity = 1;
					break;
			}
		}

		private void UpdateTitleBarLayout(CoreApplicationViewTitleBar ct)
		{
			// Get the size of the caption controls area and back button 
			// (returned in logical pixels), and move your content around as necessary.
			LeftPaddingColumn.Width = new GridLength(ct.SystemOverlayLeftInset);
			RightPaddingColumn.Width = new GridLength(ct.SystemOverlayRightInset);
		}

		public void ApplyLayout()
		{
			// Hide default title bar.			
			GetCoreTitleBar().ExtendViewIntoTitleBar = true;
			UpdateTitleBarLayout(GetCoreTitleBar());

			// Set XAML element as a draggable region.
			Window.Current.SetTitleBar(AppTitleBar);

			// And set the initial state of visibility
			//Visibility = GetCoreTitleBar().IsVisible ? Visibility.Visible : Visibility.Collapsed;
		}

		private bool SetWindowTitle(string title)
		{
			try
			{
				ApplicationView.GetForCurrentView().Title = title ?? "";
				return true;
			}
			catch
			{
				return false;
			}
		}
		#endregion

		#region override methods
		protected override void OnApplyTemplate()
		{
			AppTitleBar = this.GetTemplateChild<UIElement>(nameof(AppTitleBar)) ?? new Grid();
			ContentPresenter = this.GetTemplateChild<ContentPresenter>(nameof(ContentPresenter)) ?? new ContentPresenter();
			InnerContentPresenter = this.GetTemplateChild<ContentPresenter>(nameof(InnerContentPresenter)) ?? new ContentPresenter();
			LeftPaddingColumn = this.GetTemplateChild<ColumnDefinition>(nameof(LeftPaddingColumn)) ?? new ColumnDefinition();
			RightPaddingColumn = this.GetTemplateChild<ColumnDefinition>(nameof(RightPaddingColumn)) ?? new ColumnDefinition();
		}
		#endregion

		#region public methods
		public void ConfigureTitleBarTheme(ElementTheme theme)
		{
			Color foreground = default, background = default;

			switch (theme)
			{
				case ElementTheme.Default:
					foreground = (Color)Application.Current.Resources["SystemBaseHighColor"];
					background = (Color)Application.Current.Resources["SystemAltHighColor"];
					break;
				case ElementTheme.Light:
					foreground = Colors.Black;
					background = Colors.White;
					break;
				case ElementTheme.Dark:
					foreground = Colors.White;
					background = Colors.Black;
					break;
			}

			GetAppViewTitleBar().ForegroundColor = foreground;
			GetAppViewTitleBar().BackgroundColor = background;

			GetAppViewTitleBar().ButtonForegroundColor = foreground;
			GetAppViewTitleBar().ButtonBackgroundColor = Colors.Transparent;

			GetAppViewTitleBar().ButtonHoverForegroundColor = foreground;
			var newAlpha = (byte)(foreground.A * 0.2);
			GetAppViewTitleBar().ButtonHoverBackgroundColor = Color.FromArgb(newAlpha, foreground.R, foreground.G, foreground.B);

			GetAppViewTitleBar().ButtonPressedForegroundColor = foreground;
			newAlpha = (byte)(foreground.A * 0.4);
			GetAppViewTitleBar().ButtonPressedBackgroundColor = Color.FromArgb(newAlpha, foreground.R, foreground.G, foreground.B);

			//titleBar.ButtonInactiveForegroundColor = Colors.Gray;
			GetAppViewTitleBar().ButtonInactiveBackgroundColor = Colors.Transparent;
		}
		#endregion
	}
}