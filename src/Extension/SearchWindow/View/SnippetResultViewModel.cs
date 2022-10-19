using Community.VisualStudio.Toolkit;
using Extension.Caching;
using Extension.SnippetFormats;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Extension.SearchWindow.View
{
	internal class SnippetSearchViewModel
	{
		private string term;
		private bool allSnippets;
		private bool onlyPublic;
		private bool onlyPrivate;
		private bool onlyFavorite;

		private bool editorOpen = false;

		private GetSnippetsCommand getSnippets;
		private InsertSnippetCommand insertSnippet;
		private ShowPreviewCommand showPreview;

		// Commands
		public GetSnippetsCommand GetSnippets { get => getSnippets; set => getSnippets = value; }
		public InsertSnippetCommand InsertSnippet { get => insertSnippet; set => insertSnippet = value; }
		public ShowPreviewCommand ShowPreview { get => showPreview; set => showPreview = value; }

		// Search parameters
		public bool AllSnippets { get => allSnippets; set => allSnippets = value; }
		public string Term { get => term; set => term = value; }
		public bool OnlyFavorite { get => onlyFavorite; set => onlyFavorite = value; }
		public bool OnlyPrivate { get => onlyPrivate; set => onlyPrivate = value; }
		public bool OnlyPublic { get => onlyPublic; set => onlyPublic = value; }

		// Results
		public ObservableCollection<VisualStudioSnippet> Snippets { get; set; }

		// View behaviour
		public bool EditorOpen { get => editorOpen;}


		public SnippetSearchViewModel()
		{
			Snippets = new ObservableCollection<VisualStudioSnippet>();
			GetSnippets = new GetSnippetsCommand { ViewModel = this };
			VS.Events.DocumentEvents.Opened += DocumentEvents_Opened;
			VS.Events.DocumentEvents.Closed += DocumentEvents_Closed;
		}

		private async void DocumentEvents_Closed(string obj)
		{
			var doc = await VS.Documents.GetActiveDocumentViewAsync();
			editorOpen = doc != null;
			GetSnippets.RaiseCanExecuteChanged();
		}

		private async void DocumentEvents_Opened(string obj)
		{
			editorOpen = true;
			GetSnippets.RaiseCanExecuteChanged();
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
