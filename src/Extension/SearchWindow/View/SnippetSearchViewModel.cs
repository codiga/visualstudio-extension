using Community.VisualStudio.Toolkit;
using Extension.AssistantCompletion;
using Extension.Caching;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Editor;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Extension.SnippetFormats;

namespace Extension.SearchWindow.View
{
	internal class SnippetSearchViewModel
	{
		private ICodigaClientProvider _clientProvider;

		private string _term;
		private bool _allSnippets;
		private bool _onlyPublic;
		private bool _onlyPrivate;
		private bool _onlyFavorite;

		private bool _editorOpen = false;

		private AsyncButtonCommand _getSnippets;
		private AsyncButtonCommand _insertSnippet;
		private AsyncButtonCommand _showPreview;

		// Commands
		public AsyncButtonCommand GetSnippets { get => _getSnippets; set => _getSnippets = value; }
		public AsyncButtonCommand InsertSnippet { get => _insertSnippet; set => _insertSnippet = value; }
		public AsyncButtonCommand ShowPreview { get => _showPreview; set => _showPreview = value; }

		// Search parameters
		public bool AllSnippets { get => _allSnippets; set => _allSnippets = value; }
		public string Term { get => _term; set => _term = value; }
		public bool OnlyFavorite { get => _onlyFavorite; set => _onlyFavorite = value; }
		public bool OnlyPrivate { get => _onlyPrivate; set => _onlyPrivate = value; }
		public bool OnlyPublic { get => _onlyPublic; set => _onlyPublic = value; }

		// Results
		public ObservableCollection<VisualStudioSnippet> Snippets { get; set; }

		// View behaviour
		public bool EditorOpen { get => _editorOpen;}

		public SnippetSearchViewModel()
		{
			_clientProvider = new DefaultCodigaClientProvider();

			Snippets = new ObservableCollection<VisualStudioSnippet>();

			GetSnippets = new AsyncButtonCommand (QuerySnippetsAsync, IsEditorOpen){ ViewModel = this };
			InsertSnippet = new AsyncButtonCommand (InsertSnippetAsync, IsEditorOpen) { ViewModel = this };
			ShowPreview = new AsyncButtonCommand (InsertSnippetAsync, IsEditorOpen) { ViewModel = this };

			VS.Events.DocumentEvents.Opened += DocumentEvents_Opened;
			VS.Events.DocumentEvents.Closed += DocumentEvents_Closed;
		}

		private async void DocumentEvents_Closed(string obj)
		{
			var windows = await VS.Windows.GetAllDocumentWindowsAsync();
			_editorOpen = windows.Any();
			GetSnippets.RaiseCanExecuteChanged();
			InsertSnippet.RaiseCanExecuteChanged();
			ShowPreview.RaiseCanExecuteChanged();
		}

		private async void DocumentEvents_Opened(string obj)
		{
			_editorOpen = true;
			GetSnippets.RaiseCanExecuteChanged();
			InsertSnippet.RaiseCanExecuteChanged();
			ShowPreview.RaiseCanExecuteChanged();
		}

		#region GetSnippets command

		public bool IsEditorOpen(object param)
		{
			return EditorOpen;
		}

		public async Task QuerySnippetsAsync(object param)
		{
			var currentDocView =  await VS.Documents.GetActiveDocumentViewAsync();

			var client = _clientProvider.GetClient();
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
		#endregion

		#region InsertSnippet command

		public async Task InsertSnippetAsync(object param)
		{
			var client = new ExpansionClient();
			
			var currentDocView = await VS.Documents.GetActiveDocumentViewAsync();
			var adapterFactory = await VS.GetMefServiceAsync<IVsEditorAdaptersFactoryService>();
			var snippet = (VisualStudioSnippet)param;
			var caretPosition = currentDocView.TextView.Caret.Position;
			var legacyCaretPosition = caretPosition.GetLegacyCaretPosition();
			var vsTextView = adapterFactory.GetViewAdapter(currentDocView.TextView);
			client.StartExpansion(vsTextView, snippet, legacyCaretPosition);
		}

		#endregion

		#region ShowPreview command
		public async Task ShowPreviewAsync(object param)
		{

		}
		#endregion

	}
}
