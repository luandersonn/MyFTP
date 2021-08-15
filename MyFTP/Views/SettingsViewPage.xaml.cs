using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Messaging;
using MyFTP.Utils;
using MyFTP.ViewModels;
using System;
using System.Linq;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;

namespace MyFTP.Views
{
	public sealed partial class SettingsViewPage : Page
	{
		private int totalExpanderCount;
		private int expandedCount;
		public SettingsViewPage()
		{
			InitializeComponent();
			DataContext = App.Current.Services.GetRequiredService<SettingsViewModel>();
			Loaded += (sender, args) => WeakReferenceMessenger.Default.Register<RequestOpenFolderMessage>(this, OnOpenFolderRequest);			
			Unloaded += (sender, args) => WeakReferenceMessenger.Default.Unregister<RequestOpenFolderMessage>(this);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			totalExpanderCount = RootStackPanel.Children.OfType<muxc.Expander>().Count();
			UpdateExpanderButtons();
			if (ViewModel.UpdateService?.CheckForUpdatesCommand.CanExecute(null) == true)
				ViewModel.UpdateService.CheckForUpdatesCommand.Execute(null);
		}

		public SettingsViewModel ViewModel => DataContext as SettingsViewModel;

		#region expanders		
		public bool IsAllExpanded
		{
			get
			{
				return RootStackPanel
								.Children
								.OfType<muxc.Expander>()
								.All(expander => expander.IsExpanded);
			}
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
		private void Expanding()
		{
			expandedCount++;
			UpdateExpanderButtons();
		}
		private void Collapsed()
		{
			expandedCount--;
			UpdateExpanderButtons();
		}
		private void UpdateExpanderButtons()
		{
			if (expandedCount == 0)
				CollapseAllButton.IsEnabled = false;
			else
				CollapseAllButton.IsEnabled = true;

			if (expandedCount == totalExpanderCount)
				ExpandAllButton.IsEnabled = false;
			else
				ExpandAllButton.IsEnabled = true;
		}
		#endregion

		#region servers
		private void OnOpenFolderRequest(object recipient, RequestOpenFolderMessage message)
		{
			if (!message.HasReceivedResponse)
			{
				var picker = new FolderPicker();
				picker.FileTypeFilter.Add(".");
				message.Reply(picker.PickSingleFolderAsync().AsTask());
			}
		}
		#endregion

		#region app theme
		public int ThemeIndex
		{
			get => (int)ViewModel.AppTheme;			
			set => ViewModel.AppTheme = (ElementTheme)value;			
		}
		#endregion		

		private void GoBack()
		{
			if (Frame.CanGoBack)
				Frame.GoBack();
		}		
	}
}