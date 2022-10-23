﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Extension.SearchWindow.View
{
	internal class AsyncButtonCommand : ICommand
	{
		private readonly Func<object, Task> _action;
		private readonly Predicate<object> _canExecute;

		public SnippetSearchViewModel ViewModel { get; set; }

		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }

			remove { CommandManager.RequerySuggested -= value; }
		}

		public AsyncButtonCommand(Func<object, Task> action, Predicate<object> canExecute)
		{
			_action = action;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			return _canExecute(parameter);
		}

		public async void Execute(object parameter)
		{
			await _action(parameter);
		}

		public void RaiseCanExecuteChanged()
		{
			CommandManager.InvalidateRequerySuggested();
		}
	}
}