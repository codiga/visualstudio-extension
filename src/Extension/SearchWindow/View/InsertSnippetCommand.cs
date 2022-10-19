using System;
using System.Windows.Input;

namespace Extension.SearchWindow.View
{
	internal class InsertSnippetCommand : ICommand
	{
		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }

			remove { CommandManager.RequerySuggested -= value; }
		}
		
		public bool CanExecute(object parameter)
		{
			throw new NotImplementedException();
		}

		public void Execute(object parameter)
		{
			throw new NotImplementedException();
		}

		public void RaiseCanExecuteChanged()
		{
			CommandManager.InvalidateRequerySuggested();
		}
	}
}
