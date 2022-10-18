using Extension.Caching;
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
		public SnippetSearchViewModel ViewModel;

		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }

			remove { CommandManager.RequerySuggested -= value; }
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public async void Execute(object parameter)
		{
			await ViewModel.QuerySnippetsAsync();

			RaiseCanExecuteChanged();
		}
		public void RaiseCanExecuteChanged()
		{
			CommandManager.InvalidateRequerySuggested();
		}
	}
}
