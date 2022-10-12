using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension.InlineCompletion
{
	internal class ListNavigator<T>
	{
		private IList<T> Snippets { get; }
		private int CurrentIndex { get; set; }

		public ListNavigator(IList<T> items)
		{
			Snippets = items;
			CurrentIndex = 0;
		}

		public T First()
		{
			CurrentIndex = 0;
			return Snippets[CurrentIndex];
		}

		public T Next()
		{
			CurrentIndex++;
			if (CurrentIndex == Snippets.Count)
				CurrentIndex = 0;
			return Snippets[CurrentIndex];
		}

		public T Previous()
		{
			CurrentIndex--;
			if (CurrentIndex < 0)
				CurrentIndex = Snippets.Count - 1;
			return Snippets[CurrentIndex];
		}

	}
}
