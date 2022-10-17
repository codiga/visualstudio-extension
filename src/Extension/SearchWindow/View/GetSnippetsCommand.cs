using GraphQLClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Extension.SearchWindow.View
{
	internal class GetSnippetsCommand : ICommand
	{
		public event EventHandler CanExecuteChanged;

		public SnippetResultViewModel ViewModel;

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			var snippet = new CodigaSnippet { Name = "TestSnippet" };
			ViewModel.Snippets.Add(snippet);
		}
	}
}
