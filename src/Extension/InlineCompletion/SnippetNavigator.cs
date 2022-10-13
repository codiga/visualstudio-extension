using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension.InlineCompletion
{
	/// <summary>
	/// Helper class to support cycling through a List.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class ListNavigator<T>
	{
		private IList<T> Items { get; }
		private int CurrentIndex { get; set; }

		public T CurrentItem => Items[CurrentIndex];

		public int Count => Items.Count;

		public ListNavigator(IList<T> items)
		{
			Items = items;
			CurrentIndex = 0;
		}

		public int IndexOf(T item)
		{
			return Items.IndexOf(item);
		}

		public T First()
		{
			if (!Items.Any())
				return default(T);

			CurrentIndex = 0;
			return Items[CurrentIndex];
		}

		public T Next()
		{
			if (!Items.Any())
				return default(T);

			CurrentIndex++;
			if (CurrentIndex >= Items.Count)
				CurrentIndex = 0;
			return Items[CurrentIndex];
		}

		public T Previous()
		{
			if (!Items.Any())
				return default(T);

			CurrentIndex--;
			if (CurrentIndex < 0)
				CurrentIndex = Items.Count - 1;
			return Items[CurrentIndex];
		}
	}
}
