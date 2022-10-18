using Community.VisualStudio.Toolkit;
using Extension.Caching;
using Extension.SnippetFormats;
using GraphQLClient;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Extension.SearchWindow.View
{
	internal class SnippetSearchViewModel
	{
		private string term;
		private bool allSnippets;
		private bool onlyPublic;
		private bool onlyPrivate;
		private bool onlyFavorite;

		private bool editorOpen;

		private ICommand getSnippets;

		public ICommand GetSnippets { get => getSnippets; set => getSnippets = value; }
		public bool AllSnippets { get => allSnippets; set => allSnippets = value; }
		public string Term { get => term; set => term = value; }
		public bool OnlyFavorite { get => onlyFavorite; set => onlyFavorite = value; }
		public bool OnlyPrivate { get => onlyPrivate; set => onlyPrivate = value; }
		public bool OnlyPublic { get => onlyPublic; set => onlyPublic = value; }
		public ObservableCollection<VisualStudioSnippet> Snippets { get; set; }
		public bool EditorOpen { get => editorOpen; set => editorOpen = value; }

		public SnippetSearchViewModel()
		{
			Snippets = new ObservableCollection<VisualStudioSnippet>();
			GetSnippets = new GetSnippetsCommand { ViewModel = this };
		}

		public async Task QuerySnippetsAsync()
		{
			var currentDocView =  await VS.Documents.GetActiveDocumentViewAsync();

			using var client = new CodigaClientProvider().GetClient();
			var ext = Path.GetExtension(currentDocView.Document.FilePath);
			var languages = new ReadOnlyCollection<string>(new[] { CodigaLanguages.Parse(ext) });

			var result = await client.GetRecipesForClientSemanticAsync(Term, languages, OnlyPublic, null, OnlyFavorite, 10, 0);

			var settings = EditorSettingsProvider.GetCurrentIndentationSettings();
			var vsSnippets = result.Select(s => SnippetParser.FromCodigaSnippet(s, settings));

			Snippets.Clear();

			foreach (var snippet in vsSnippets)
			{
				Snippets.Add(snippet);
			}
		}
	}
}
