using Microsoft.Toolkit.Uwp;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.System;

namespace MyFTP.Utils
{
	public abstract class BindableItem : INotifyPropertyChanged
	{
		protected DispatcherQueue Dispatcher { get; set; }
		protected BindableItem() { }
		protected BindableItem(DispatcherQueue dispatcher) => Dispatcher = dispatcher;

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual bool Set<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(storage, value))
				return false;
			storage = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		protected async Task AccessUIAsync(Action action, DispatcherQueuePriority priority = DispatcherQueuePriority.Low) => await Dispatcher.EnqueueAsync(() => action(), priority);
		protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
