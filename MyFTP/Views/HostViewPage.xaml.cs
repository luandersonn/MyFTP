using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.Controls;
using MyFTP.Utils;
using MyFTP.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.ViewManagement;
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
			ViewModel = App.Current.Services.GetRequiredService<HostViewModel>();
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
				this.AddKeyboardAccelerator(VirtualKey.N, VirtualKeyModifiers.Control, OnAcceleratorRequested);
				this.AddKeyboardAccelerator(VirtualKey.O, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, OnAcceleratorRequested);
				this.AddKeyboardAccelerator(VirtualKey.W, VirtualKeyModifiers.Control, OnAcceleratorRequested);
				this.AddKeyboardAccelerator(VirtualKey.F11, OnAcceleratorRequested);

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


		public HostViewModel ViewModel { get; }
		public ObservableCollection<FtpListItemViewModel> Crumbs { get; }
		public NavigationHistory<FtpListItemViewModel> NavigationHistory { get; }

		protected async override void OnNavigatedTo(NavigationEventArgs args)
		{
			_frame.Navigate(typeof(FtpDirectoryViewPage));
			if (args.NavigationMode == NavigationMode.New)
			{
				try
				{
					var root = args.Parameter as FtpListItemViewModel;
					if (root == null)
						throw new InvalidOperationException("Invalid param");
					ViewModel.AddItem(root);
					await Task.Delay(500);
					treeView.SelectedNode = treeView.RootNodes.FirstOrDefault(x => x.Content == root);
				}
				catch (Exception e)
				{
					ShowError(e.Message, e);
				}
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
			else if (treeView.RootNodes.Count == 0) // No Hosts! Back to login page
			{
				Frame.GoBack();
			}
		}

		private void OnCoreWindowPointerPressed(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
		{
			if (args.CurrentPoint.Properties.IsXButton1Pressed)
			{
				// Mouse back button pressed
				if (NavigationHistory.GoBack())
				{
					args.Handled = true;
				}

			}
			else if (args.CurrentPoint.Properties.IsXButton2Pressed)
			{
				// Mouse forward button pressed				
				args.Handled = NavigationHistory.GoForward();
			}
			if (args.Handled)
			{
				// Due TreeView bug, you need set the node manually :(
				if (NavigationHistory.CurrentItem != null && NavigationHistory.CurrentItem.Parent == null) // Root item!
				{
					treeView.SelectedNode = treeView.RootNodes.FirstOrDefault(x => x.Content == NavigationHistory.CurrentItem);
				}
			}
		}

		private async void OnAcceleratorRequested(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			switch (args.KeyboardAccelerator.Key)
			{
				case VirtualKey.Back when NavigationHistory.CurrentItem?.Parent != null: // Go up
					if (NavigationHistory.CurrentItem.Parent.Parent == null) // Root
					{
						treeView.SelectedNode = treeView.RootNodes.FirstOrDefault(x => x.Content == NavigationHistory.CurrentItem.Parent);
					}
					else
					{
						treeView.SelectedItem = NavigationHistory.CurrentItem.Parent;
					}
					args.Handled = true;
					break;

				case VirtualKey.Left when args.KeyboardAccelerator.Modifiers == VirtualKeyModifiers.Menu:
					args.Handled = NavigationHistory.GoBack();
					break;

				case VirtualKey.Right when args.KeyboardAccelerator.Modifiers == VirtualKeyModifiers.Menu:
					args.Handled = NavigationHistory.GoForward();
					break;

				case VirtualKey.N when args.KeyboardAccelerator.Modifiers == VirtualKeyModifiers.Control:
					args.Handled = true;
					await NewConnectionAsync();
					break;

				case VirtualKey.O when args.KeyboardAccelerator.Modifiers == (VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift)
												&& treeView.SelectedItem is FtpListItemViewModel dir
												&& dir.IsDirectory
												&& dir.UploadFolderCommand.CanExecute(null):
					dir.UploadFolderCommand.Execute(null);
					args.Handled = true;
					break;

				case VirtualKey.W when args.KeyboardAccelerator.Modifiers == VirtualKeyModifiers.Control
												&& ViewModel.DisconnectCommand.CanExecute(treeView.SelectedItem):
					ViewModel.DisconnectCommand.Execute(treeView.SelectedItem);
					args.Handled = true;
					break;

				case VirtualKey.F11:
					FullScreenToggle();
					args.Handled = true;
					break;
			}
		}

		private void FullScreenToggle()
		{
			var view = ApplicationView.GetForCurrentView();
			if (view.IsFullScreenMode)
				view.ExitFullScreenMode();
			else
				view.TryEnterFullScreenMode();
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
			var item = Crumbs.Reverse().Skip(1).FirstOrDefault();
			if (item == null)
				treeView.SelectedNode = treeView.RootNodes.FirstOrDefault();
			else if (item.Parent == null) // root #BUG 
				treeView.SelectedNode = treeView.RootNodes.FirstOrDefault(x => x.Content == item);
			else
				treeView.SelectedItem = item;
		}
		private void OnBreadcrumbBarItemClicked(muxc.BreadcrumbBar sender, muxc.BreadcrumbBarItemClickedEventArgs args)
		{
			// #BUG 
			if (args.Index == 0)
			{
				treeView.SelectedNode = treeView.RootNodes.FirstOrDefault(x => x.Content == args.Item);
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

		private async Task NewConnectionAsync()
		{
			var dialog = new LoginDialog
			{
				RequestedTheme = ActualTheme
			};
			if (await dialog.ShowAsync() == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
			{
				ViewModel.AddItem(dialog.Result);
				await Task.Delay(200);
				treeView.SelectedNode = treeView.RootNodes.FirstOrDefault(x => x.Content == dialog.Result);
			}
		}

		private void GoBack()
		{
			if (Frame.CanGoBack)
				Frame.GoBack();
		}
		private void GoToSettings() => Frame.Navigate(typeof(SettingsViewPage));

		private void ExitApp() => Application.Current.Exit();

        private void SwipeBack(SwipeItem sender, SwipeItemInvokedEventArgs args)
        {
			var item = Crumbs.Reverse().Skip(1).FirstOrDefault();
			if (item == null)
				treeView.SelectedNode = treeView.RootNodes.FirstOrDefault();
			else if (item.Parent == null) // root #BUG 
				treeView.SelectedNode = treeView.RootNodes.FirstOrDefault(x => x.Content == item);
			else
				treeView.SelectedItem = item;
		}
    }
}