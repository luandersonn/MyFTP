using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.Services;
using MyFTP.Utils;
using MyFTP.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;
namespace MyFTP.Views
{
	public sealed partial class HostViewPage : Page
	{
		private long _onTreeViewSelectedItemChangedToken;
		public HostViewPage()
		{
			InitializeComponent();
			Crumbs = new ObservableCollection<FtpListItemViewModel>();
			NavigationHistory = new NavigationHistory<FtpListItemViewModel>();
			Loaded += (sender, args) =>
			{
				WeakReferenceMessenger.Default.Register<RequestOpenFilesMessage>(this, OnOpenFileRequest);
				WeakReferenceMessenger.Default.Register<RequestSaveFileMessage>(this, OnSaveFileRequest);
				WeakReferenceMessenger.Default.Register<RequestOpenFolderMessage>(this, OnOpenFolderRequest);
				WeakReferenceMessenger.Default.Register<ErrorMessage>(this, OnErrorMessage);
				WeakReferenceMessenger.Default.Register<SelectedItemChangedMessage<FtpListItemViewModel>>(this, OnSelectedItemChanged);
				_onTreeViewSelectedItemChangedToken = treeView.RegisterPropertyChangedCallback(muxc.TreeView.SelectedItemProperty, OnSelectedItemChanged);
				Window.Current.CoreWindow.PointerPressed += OnCoreWindowPointerPressed;
				this.AddKeyboardAccelerator(VirtualKey.Back, OnAcceleratorRequested);
				this.AddKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu, OnAcceleratorRequested);
				this.AddKeyboardAccelerator(VirtualKey.Right, VirtualKeyModifiers.Menu, OnAcceleratorRequested);
				IconRotation.Begin();
			};
			Unloaded += (sender, args) =>
			{
				WeakReferenceMessenger.Default.Unregister<RequestOpenFilesMessage>(this);
				WeakReferenceMessenger.Default.Unregister<RequestSaveFileMessage>(this);
				WeakReferenceMessenger.Default.Unregister<RequestOpenFolderMessage>(this);
				WeakReferenceMessenger.Default.Unregister<ErrorMessage>(this);
				WeakReferenceMessenger.Default.Unregister<SelectedItemChangedMessage<FtpListItemViewModel>>(this);
				treeView.UnregisterPropertyChangedCallback(muxc.TreeView.SelectedItemProperty, _onTreeViewSelectedItemChangedToken);
				Window.Current.CoreWindow.PointerPressed -= OnCoreWindowPointerPressed;
				IconRotation.Stop();
			};
		}

		public HostViewModel ViewModel { get => (HostViewModel)GetValue(ViewModelProperty); set => SetValue(ViewModelProperty, value); }
		public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
			typeof(HostViewModel), typeof(HostViewPage), new PropertyMetadata(null));

		public ObservableCollection<FtpListItemViewModel> Crumbs { get; }
		public NavigationHistory<FtpListItemViewModel> NavigationHistory { get; }

		protected async override void OnNavigatedTo(NavigationEventArgs args)
		{
			try
			{
				_frame.Navigate(typeof(FtpDirectoryViewPage));
				ViewModel = args.Parameter as HostViewModel;
				if (ViewModel == null)
					throw new InvalidOperationException("Invalid param");
				await ViewModel.LoadRootAsync();
				treeView.SelectedNode = treeView.RootNodes.FirstOrDefault();
			}
			catch (Exception e)
			{
				ShowError(e.Message, e);
			}
		}


		private void OnOpenFileRequest(object recipient, RequestOpenFilesMessage message)
		{
			if (!message.HasReceivedResponse)
			{
				var filePicker = new FileOpenPicker();
				filePicker.FileTypeFilter.Add("*");
				message.Reply(filePicker.PickMultipleFilesAsync().AsTask());
			}
		}

		private void OnSaveFileRequest(object recipient, RequestSaveFileMessage message)
		{
			if (!message.HasReceivedResponse)
			{
				var picker = new FileSavePicker();
				picker.FileTypeChoices.Add("File", new string[] { "." });
				picker.SuggestedFileName = message.FileNameSuggestion ?? "";
				message.Reply(picker.PickSaveFileAsync().AsTask());
			}
		}

		private void OnOpenFolderRequest(object recipient, RequestOpenFolderMessage message)
		{
			if (!message.HasReceivedResponse)
			{
				var picker = new FolderPicker();
				picker.FileTypeFilter.Add(".");
				message.Reply(picker.PickSingleFolderAsync().AsTask());
			}
		}

		private void OnErrorMessage(object recipient, ErrorMessage message)
		{
			ShowError(message.Exception.Message, message.Exception);
		}

		private void OnSelectedItemChanged(object recipient, SelectedItemChangedMessage<FtpListItemViewModel> message)
		{
			treeView.SelectedItem = message.Item;
		}

		private void OnSelectedItemChanged(DependencyObject d, DependencyProperty args)
		{
			var item = (FtpListItemViewModel)d.GetValue(args);

			if (item != null)
			{
				if (item != NavigationHistory.CurrentItem)
				{
					if (NavigationHistory.CanGoForward)
						NavigationHistory.NavigateTo(item, NavigationHistory.CurrentItemIndex + 1);
					else
						NavigationHistory.NavigateTo(item);
				}
				// Update the BreadcrumbBar
				Crumbs.Clear();
				var crumb = item;
				do
				{
					Crumbs.Insert(0, crumb);
					crumb = crumb.Parent;
				} while (crumb != null);
				WeakReferenceMessenger.Default.Send(new SelectedItemChangedMessage<FtpListItemViewModel>(this, item));
			}
		}

		private void OnCoreWindowPointerPressed(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
		{
			if (args.CurrentPoint.Properties.IsXButton1Pressed)
			{
				// Mouse back button pressed
				args.Handled = NavigationHistory.GoBack();
			}
			else if (args.CurrentPoint.Properties.IsXButton2Pressed)
			{
				// Mouse forward button pressed				
				args.Handled = NavigationHistory.GoForward();
			}
		}

		private void OnAcceleratorRequested(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			switch (args.KeyboardAccelerator.Key)
			{
				case VirtualKey.Back when NavigationHistory.CurrentItem?.Parent != null: // Go up
					treeView.SelectedItem = NavigationHistory.CurrentItem.Parent;
					args.Handled = true;
					break;

				case VirtualKey.Left when args.KeyboardAccelerator.Modifiers == VirtualKeyModifiers.Menu:
					args.Handled = NavigationHistory.GoBack();
					break;

				case VirtualKey.Right when args.KeyboardAccelerator.Modifiers == VirtualKeyModifiers.Menu:
					args.Handled = NavigationHistory.GoForward();
					break;
			}
		}

		private async void OnDisconnectButtonClick(muxc.SplitButton sender, muxc.SplitButtonClickEventArgs args)
		{
			try
			{
				await ViewModel.DisconnectAsync();
			}
			finally
			{
				Frame.GoBack();
			}
		}

		private void OnRadioThemeLoaded(object sender, RoutedEventArgs e)
		{
			var radio = (muxc.RadioMenuFlyoutItem)sender;
			var radioTheme = ((ElementTheme)Enum.Parse(typeof(ElementTheme), radio.Tag.ToString())).ToString();
			if (Frame.RequestedTheme.ToString() == radioTheme)
			{
				radio.IsChecked = true;
			}
		}

		private void OnRadioActualThemeChanged(FrameworkElement sender, object args) => OnRadioThemeLoaded(sender, null);

		private void OnRadioThemeClick(object sender, RoutedEventArgs e)
		{
			var radio = (muxc.RadioMenuFlyoutItem)sender;
			var radioTheme = ((ElementTheme)Enum.Parse(typeof(ElementTheme), radio.Tag.ToString()));
			var settings = App.Current.Services.GetService<ISettings>();
			if (settings != null)
				settings.TrySet("AppTheme", radioTheme);
		}


		private void ShowError(string message, Exception e = null)
		{
			infoBar.IsOpen = false;
			infoBar.Message = message;
			infoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
			infoBar.IsOpen = true;
			Debug.WriteLineIf(e != null, e);
		}

		private void OnButtonUpClicked(object sender, RoutedEventArgs args)
		{
			treeView.SelectedItem = Crumbs.Reverse().Skip(1).FirstOrDefault();
		}
		private void OnBreadcrumbBarItemClicked(muxc.BreadcrumbBar sender, muxc.BreadcrumbBarItemClickedEventArgs args)
		{
			// #BUG 
			if (args.Index == 0)
			{
				treeView.SelectedNode = treeView.RootNodes.FirstOrDefault();
			}
			else
				treeView.SelectedItem = args.Item;
		}

		private async void OnListViewContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.ItemContainer.ContentTemplateRoot is Grid root
							&& args.Item is FtpListItemViewModel item
							&& root.Children.OfType<Image>().FirstOrDefault() is Image image)
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
							StorageItemThumbnail thumbnail;
							try
							{
								switch (item.Type)
								{
									case FluentFTP.FtpFileSystemObjectType.File:
										thumbnail = await Utils.IconHelper.GetFileIconAsync(Path.GetExtension(item.Name));
										break;
									case FluentFTP.FtpFileSystemObjectType.Directory:
										thumbnail = await Utils.IconHelper.GetFolderIconAsync();
										break;
									default:
										return;
								}
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

		private void OpenCloseTransferList() => transferTeachingTip.IsOpen = !transferTeachingTip.IsOpen;

		private void OnListViewItemClick(object sender, ItemClickEventArgs e)
		{
			treeView.SelectedItem = e.ClickedItem;
		}

		private void ExitApp() => Application.Current.Exit();
	}
}