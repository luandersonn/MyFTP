using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MyFTP.Collections
{
	public interface IObservableSortedCollection<T> : INotifyPropertyChanged, INotifyCollectionChanged, IEnumerable<T>
	{
		IComparer<T> Comparer { get; }
		T this[int index] { get; set; }
		void AddItem(T item);
		bool RemoveItem(T item);
		int BinarySearch(T item);
		void Clear();
		int Count { get; }
	}
}
