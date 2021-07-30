using Microsoft.Toolkit.Mvvm.Input;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace MyFTP.Controls
{
	public sealed partial class RenameItemDialog : ContentDialog
	{
		public RenameItemDialog(IAsyncRelayCommand renameCommand, string originalName)
		{
			InitializeComponent();
			Opened += OnOpened;
			Closing += OnClosing;
			RenameCommand = renameCommand ?? throw new ArgumentNullException(nameof(renameCommand));
			DataContext = ItemName = originalName;
		}

		public IAsyncRelayCommand RenameCommand { get; }
		public string ItemName { get => (string)GetValue(ItemNameProperty); set => SetValue(ItemNameProperty, value); }
		public static readonly DependencyProperty ItemNameProperty = DependencyProperty.Register("ItemName",
			typeof(string), typeof(RenameItemDialog), new PropertyMetadata(string.Empty, OnRenameItemChanged));

		private static void OnRenameItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var rid = (RenameItemDialog)d;
			rid.IsPrimaryButtonEnabled = rid.RenameCommand.CanExecute(e.NewValue);
		}

		private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
		{
			RenameCommand.CanExecuteChanged += RenameCommandCanExecuteChanged;
			IsPrimaryButtonEnabled = RenameCommand.CanExecute(PrimaryButtonCommandParameter);
			// Wait before close
			await System.Threading.Tasks.Task.Delay(25);
			if (sucessoInfoBar != null)
				sucessoInfoBar.IsOpen = false;
			if (erroInfoBar != null)
				erroInfoBar.IsOpen = false;
		}

		private void OnClosing(ContentDialog sender, ContentDialogClosingEventArgs args) => RenameCommand.CanExecuteChanged -= RenameCommandCanExecuteChanged;

		private void RenameCommandCanExecuteChanged(object sender, EventArgs e) => IsPrimaryButtonEnabled = RenameCommand.CanExecute(ItemName);

		private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			args.Cancel = true;
			if (RenameCommand.CanExecute(ItemName) == true)
				RenameCommand.Execute(ItemName);
		}

		private void RenameBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			if (RenameCommand.CanExecute(ItemName) == true)
				RenameCommand.Execute(ItemName);
		}
		private void RenameBoxKeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Escape)
				Hide();
		}
	}
}
