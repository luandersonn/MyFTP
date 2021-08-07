using Microsoft.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using muxc = Microsoft.UI.Xaml.Controls;

namespace MyFTP.Utils
{
	public class DragAndDropHelper
	{
		#region enabled drag items from app
		public static string DragItemsFormatId { get; } = "DragItemsFormatId";
		public static bool GetIsDragItemsEnabled(UIElement element) => (bool)element.GetValue(IsDragItemsEnabledProperty);
		public static void SetIsDragItemsEnabled(UIElement element, bool value)
		{
			element.SetValue(IsDragItemsEnabledProperty, value);
			switch (element)
			{
				case ListViewBase lvb when value:
					lvb.CanDragItems = true;
					lvb.DragItemsStarting += OnListviewDragItemsStarting;
					lvb.DragItemsCompleted += OnListviewDragItemsCompleted;
					break;

				case muxc.TreeView tv when value:
					tv.CanDragItems = true;
					tv.DragItemsStarting += OnTreeviewDragItemsStarting;
					tv.DragItemsCompleted += OnTreeviewDragItemsCompleted;
					break;

				case ListViewBase lvb:
					lvb.CanDragItems = false;
					lvb.DragItemsStarting -= OnListviewDragItemsStarting;
					lvb.DragItemsCompleted -= OnListviewDragItemsCompleted;
					break;

				case muxc.TreeView tv:
					tv.CanDragItems = false;
					tv.DragItemsStarting -= OnTreeviewDragItemsStarting;
					tv.DragItemsCompleted -= OnTreeviewDragItemsCompleted;
					break;
			}
		}
		public static readonly DependencyProperty IsDragItemsEnabledProperty = DependencyProperty.RegisterAttached("IsDragItemsEnabled", typeof(bool), typeof(DragAndDropHelper), new PropertyMetadata(false));

		private static void OnListviewDragItemsStarting(object sender, DragItemsStartingEventArgs args)
		{
			// Need to contains IDragTarget
			args.Cancel = !args.Items.Any(x => x is IDragTarget);
			args.Data.Properties.Add(DragItemsFormatId, args.Items);
			args.Data.SetData(DragItemsFormatId, DragItemsFormatId);
		}

		private static void OnListviewDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
		{
			
		}

		private static void OnTreeviewDragItemsStarting(muxc.TreeView sender, muxc.TreeViewDragItemsStartingEventArgs args)
		{
			// Need to contains IDragTarget
			args.Cancel = !args.Items.Any(x => x is IDragTarget);
			args.Data.Properties.Add(DragItemsFormatId, args.Items);
			args.Data.SetData(DragItemsFormatId, DragItemsFormatId);
		}

		private static void OnTreeviewDragItemsCompleted(muxc.TreeView sender, muxc.TreeViewDragItemsCompletedEventArgs args)
		{
			
		}

		#endregion

		#region enabled drop from app and system
		public static bool GetIsDropItemsEnabled(UIElement element) => (bool)element.GetValue(IsDropItemsEnabledProperty);
		public static void SetIsDropItemsEnabled(UIElement element, bool value)
		{
			element.SetValue(IsDropItemsEnabledProperty, value);
			if (value)
			{
				element.AllowDrop = true;
				element.DragOver += OnElementDragEnter;
				element.DragLeave += OnElementLeave;
				element.Drop += OnElementDrop;
			}
			else
			{
				element.AllowDrop = false;
				element.DragOver -= OnElementDragEnter;
				element.DragLeave -= OnElementLeave;
				element.Drop -= OnElementDrop;
			}
		}

		private static void OnElementDragEnter(object sender, DragEventArgs args)
		{
			var element = (UIElement)sender;
			var target = GetDropTarget(element) as IDropTarget;
			if (target == null)
				return;

			if (args.DataView.Contains(DragItemsFormatId)
							&& args.DataView.Properties.TryGetValue(DragItemsFormatId, out var value)
							&& value is IList<object> list) // dragging items from app
			{			
				args.AcceptedOperation = DataPackageOperation.Move;
				args.DragUIOverride.Caption = string.Format("Move to: {0}", target.Name.Truncate(80, true));
			}

			else if (args.DataView.Contains(StandardDataFormats.StorageItems)) // dragging files from system
			{
				args.AcceptedOperation = DataPackageOperation.Copy;
				args.DragUIOverride.Caption = string.Format("Upload to: {0}", target.Name.Truncate(80, true));
			}
			else
				args.AcceptedOperation = DataPackageOperation.None;
		}

		private static void OnElementLeave(object sender, DragEventArgs e)
		{

		}

		private async static void OnElementDrop(object sender, DragEventArgs args)
		{
			var element = (UIElement)sender;
			var target = GetDropTarget(element) as IDropTarget;
			if (target == null)
				return;
			if (args.DataView.Contains(DragItemsFormatId)
							&& args.DataView.Properties.TryGetValue(DragItemsFormatId, out var value)
							&& value is IList<object> list) // dragging items from app
			{
				var items = list.OfType<IDragTarget>().Where(item => target.IsDragItemSupported(item));
				target.DropItems(items);
			}
			else if (args.DataView.Contains(StandardDataFormats.StorageItems)) // dragging files from system
			{
				var items = await args.DataView.GetStorageItemsAsync();
				target.DropItems(items);
			}
		}
		public static readonly DependencyProperty IsDropItemsEnabledProperty = DependencyProperty.RegisterAttached("IsDropItemsEnabled", typeof(bool), typeof(DragAndDropHelper), new PropertyMetadata(false));
		public static IDropTarget GetDropTarget(UIElement obj) => (IDropTarget)obj.GetValue(DropTargetProperty);
		public static void SetDropTarget(UIElement obj, IDropTarget value) => obj.SetValue(DropTargetProperty, value);
		public static readonly DependencyProperty DropTargetProperty = DependencyProperty.RegisterAttached("DropTarget", typeof(IDropTarget), typeof(DragAndDropHelper), new PropertyMetadata(null));
		#endregion

	}

	public interface IDropTarget
	{
		string Name { get; }
		void DropItems(IEnumerable<IDragTarget> items);
		void DropItems(IReadOnlyList<IStorageItem> items);
		bool IsDragItemSupported(IDragTarget item);
	}
	public interface IDragTarget
	{
		string Name { get; }
	}
}
