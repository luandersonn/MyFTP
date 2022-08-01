using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI;
using MyFTP.Services;
using MyFTP.Utils;
using MyFTP.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;

namespace MyFTP.Views
{
	public sealed partial class SettingsViewPage : Page
	{
		private int totalExpanderCount;
		private int expandedCount;

		public AppCenterService AppCenterService { get; }
		public SettingsViewPage()
		{
			InitializeComponent();

			DataContext = App.Current.Services.GetRequiredService<SettingsViewModel>();
			AppCenterService = App.Current.Services.GetService<AppCenterService>();
			Loaded += (sender, args) =>
			{
				WeakReferenceMessenger.Default.Register<RequestOpenFolderMessage>(this, OnOpenFolderRequest);
				Window.Current.CoreWindow.PointerPressed += OnCoreWindowPointerPressed;
				this.AddKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu, (s, e) => e.Handled = GoBack());
			};
			Unloaded += (sender, args) =>
			{
				WeakReferenceMessenger.Default.Unregister<RequestOpenFolderMessage>(this);
				Window.Current.CoreWindow.PointerPressed -= OnCoreWindowPointerPressed;
			};
		}

		private void OnCoreWindowPointerPressed(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
		{
			if (args.CurrentPoint.Properties.IsXButton1Pressed && Frame.CanGoBack)
			{
				Frame.GoBack();
				args.Handled = true;
			}
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (ViewModel.UpdateService?.CheckForUpdatesCommand.CanExecute(null) == true)
				ViewModel.UpdateService.CheckForUpdatesCommand.Execute(null);

#if DEBUG
			FindName(nameof(DebugExpander));
#endif
		}

		public SettingsViewModel ViewModel => DataContext as SettingsViewModel;

		#region expanders		
		public bool IsAllExpanded
		{
			get => RootStackPanel
								.Children
								.OfType<muxc.Expander>()
								.All(expander => expander.IsExpanded);
			set
			{
				foreach (var element in RootStackPanel
												.Children
												.OfType<muxc.Expander>())
				{
					element.IsExpanded = value;
				}
			}
		}
		private void ExpandAll() => IsAllExpanded = true;
		private void CollapseAll() => IsAllExpanded = false;
		private void Expanding(muxc.Expander sender, muxc.ExpanderExpandingEventArgs args)
		{
			var tag = (string)sender.Tag;
			SaveExpanderState(tag, true);
			expandedCount++;
			UpdateExpanderButtons();

			switch (tag)
			{
				case "Logs":
					ViewModel.RefreshLogsListCommand.Execute(null);
					break;

				case "Host":
					ViewModel.RefreshHostSettingsCommand.Execute(null);
					break;
			}

		}
		private void Collapsed(muxc.Expander sender, muxc.ExpanderCollapsedEventArgs args)
		{
			SaveExpanderState((string)sender.Tag, false);
			expandedCount--;
			UpdateExpanderButtons();
		}

		private void ExpanderLoaded(object sender, RoutedEventArgs args)
		{
			var expander = (muxc.Expander)sender;
			expander.IsExpanded = GetExpanderState((string)expander.Tag);
			totalExpanderCount++;
			UpdateExpanderButtons();
		}
		private void UpdateExpanderButtons()
		{
			CollapseAllButton.IsEnabled = expandedCount != 0;
			ExpandAllButton.IsEnabled = expandedCount != totalExpanderCount;
		}

		private void SaveExpanderState(string id, bool state) => ViewModel.Settings.TrySet(id, state, "ExpanderState");
		private bool GetExpanderState(string id)
		{
			bool isExpanded = false;
			ViewModel.Settings.TryGet(id, out isExpanded, "ExpanderState");
			return isExpanded;
		}
		#endregion

		#region Hosts
		private void OnOpenFolderRequest(object recipient, RequestOpenFolderMessage message)
		{
			if (!message.HasReceivedResponse)
			{
				var picker = new FolderPicker();
				picker.FileTypeFilter.Add(".");
				message.Reply(picker.PickSingleFolderAsync().AsTask());
			}
		}

		private void OnDeleteHostClicked(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var item = (FtpHostSettingsViewModel)button.DataContext;
			ViewModel.FtpHostSettingsList.Remove(item);
		}
		#endregion

		#region app theme
		public int ThemeIndex
		{
			get => (int)ViewModel.AppTheme;
			set => ViewModel.AppTheme = (ElementTheme)value;
		}
		#endregion

		private async void OnReviewButtonClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				IsEnabled = false;
				await SystemInformation.LaunchStoreForReviewAsync();
				AppCenterService?.TrackEvent("LaunchStoreForReviewAsync");
			}
			finally
			{
				IsEnabled = true;
			}
		}

		#region DEBUG		
		private async void ComboBox_Loaded(object sender, RoutedEventArgs e)
		{
			var comboBox = (ComboBox)sender;
			comboBox.ItemsSource = Windows.Globalization.ApplicationLanguages.ManifestLanguages.Prepend("Default");
			// give time to comboBox load items
			await Task.Delay(TimeSpan.FromMilliseconds(200));
			var lang = Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
			if (lang == "")
				lang = "Default";
			comboBox.SelectedItem = lang;
		}
		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = e.AddedItems[0].ToString();
			if (item == "Default")
				item = "";
			Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = item;
		}
		#endregion

		private async void OnListViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.ItemContainer.ContentTemplateRoot is Grid root
							&& args.Item is StorageFile file
							&& root.FindDescendant<HyperlinkButton>().Content is Image image)
			{

				if (args.InRecycleQueue)
				{
					image.Source = null;
				}
				else
				{
					args.Handled = true;
					switch (args.Phase)
					{
						case 0:
							args.RegisterUpdateCallback(1, OnListViewContainerContentChanging);
							break;

						case 1:

							var source = new BitmapImage
							{
								DecodePixelType = DecodePixelType.Logical
							};
							image.Source = source;

							try
							{
								var thumbnail = await Utils.IconHelper.GetFileIconAsync(file.FileType, 256);
								thumbnail.Seek(0);
								await source.SetSourceAsync(thumbnail);
							}
							catch (Exception e)
							{
								Debug.WriteLine(e);
							}
							break;
					}
				}
			}
		}

		private bool GoBack()
		{
			if (Frame.CanGoBack)
			{
				Frame.GoBack();
				return true;
			}
			return false;
		}
	}
}