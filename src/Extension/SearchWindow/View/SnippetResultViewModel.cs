using Extension.SearchWindow.View;
using GraphQLClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Extension.SearchWindow.View
{
	internal class SnippetResultViewModel
	{
		private ICommand getSnippets;
		private string recipeName;


		public ObservableCollection<CodigaSnippet> Snippets { get; set; }

		public SnippetResultViewModel()
		{
			Snippets = new ObservableCollection<CodigaSnippet>();
			GetSnippets = new GetSnippetsCommand { ViewModel = this };
		}

		public string RecipeName { get => recipeName; set => recipeName = value; }
		public ICommand GetSnippets { get => getSnippets; set => getSnippets = value; }
	}
}
