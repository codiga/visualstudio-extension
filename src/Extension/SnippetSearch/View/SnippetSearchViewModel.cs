using Community.VisualStudio.Toolkit;
using Extension.AssistantCompletion;
using Extension.Caching;
using Extension.SnippetFormats;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Extension.SnippetSearch.Preview;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Extension.Settings;
using System.Diagnostics;
using System.Windows.Navigation;

namespace Extension.SearchWindow.View
{
	internal class SnippetSearchViewModel : INotifyPropertyChanged
	{
		private ICodigaClientProvider _clientProvider;

		private string _term;
		private bool _allSnippets;
		private bool _onlyPublic;
		private bool _onlyPrivate;
		private bool _onlyFavorite;
		private string _userName;

		private bool _editorOpen = false;
		private string _watermark;

		private AsyncCommand _getSnippets;
		private AsyncCommand _insertSnippet;
		private AsyncCommand _showPreview;
		private AsyncCommand _hidePreview;
		private AsyncCommand _keyDown;
		private AsyncCommand _openProfile;

		public event PropertyChangedEventHandler PropertyChanged;

		// Commands
		public AsyncCommand GetSnippets { get => _getSnippets; set => _getSnippets = value; }
		public AsyncCommand InsertSnippet { get => _insertSnippet; set => _insertSnippet = value; }
		public AsyncCommand ShowPreview { get => _showPreview; set => _showPreview = value; }
		public AsyncCommand HidePreview { get => _hidePreview; set => _hidePreview = value; }
		public AsyncCommand KeyDown { get => _keyDown; set => _keyDown = value; }
		public AsyncCommand OpenProfile { get => _openProfile; set => _openProfile = value; }

		// Search parameters
		public string Term { get => _term; set => _term = value; }
		public bool OnlyFavorite
		{
			get => _onlyFavorite; 
			
			set
			{
				_onlyFavorite = value;
			}
		}
		public bool OnlyPrivate
		{
			get => _onlyPrivate; 
			
			set
			{
				_onlyPrivate = value;
			}
		}
		public bool OnlyPublic
		{
			get => _onlyPublic; 
			
			set
			{
				_onlyPublic = value;
			}
		}

		public bool AllSnippets
		{
			get
			{
				return _allSnippets;
			}
			set
			{
				if (value)
				{
					OnlyPrivate = false;
					OnlyPublic = false;
				}
				
				_allSnippets = value;
			}
		}

		// Results
		public ObservableCollection<VisualStudioSnippet> Snippets { get; set; }

		// View behaviour
		public string UserName { get => _userName; set => _userName = value; }

		public bool EditorOpen { 
			get 
			{
				return _editorOpen;
			}
			set
			{
				if (_editorOpen != value) 
				{ 
					_editorOpen = value;
					OnPropertyChanged();
					OnEditorOpenChanged();
				}
			}
		}

		public string Watermark
		{
			get
			{
				return _watermark;
			}
			set
			{	if (_watermark != value)
				{
					_watermark = value;
					OnPropertyChanged();
				}
			}
		}

		public SnippetSearchViewModel()
		{
			_clientProvider = new DefaultCodigaClientProvider();

			Snippets = new ObservableCollection<VisualStudioSnippet>();

			GetSnippets = new AsyncCommand (QuerySnippetsAsync, IsEditorOpen){ ViewModel = this };
			InsertSnippet = new AsyncCommand (InsertSnippetAsync, IsEditorOpen) { ViewModel = this };
			ShowPreview = new AsyncCommand (ShowPreviewAsync, IsEditorOpen) { ViewModel = this };
			HidePreview = new AsyncCommand (HidePreviewAsync, IsEditorOpen) { ViewModel = this };
			KeyDown = new AsyncCommand (OnKeyUp, IsEditorOpen) { ViewModel = this };
			OpenProfile = new AsyncCommand(OpenProfileInBrowser, IsValidProfile) { ViewModel= this };

			VS.Events.DocumentEvents.Opened += DocumentEvents_Opened;
			VS.Events.DocumentEvents.Closed += DocumentEvents_Closed;

			var windows = ThreadHelper.JoinableTaskFactory.Run( async () =>
			{
				return await VS.Windows.GetAllDocumentWindowsAsync();
			});

			var result = ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				var provider = new DefaultCodigaClientProvider();
				var client = provider.GetClient();
				return await client.GetUserAsync();
			});

			UserName = result.Data?.User?.UserName ?? "";

			EditorOpen = windows.Any();
			OnEditorOpenChanged();

			AllSnippets = true;
		}


		private async void DocumentEvents_Closed(string obj)
		{
			var windows = await VS.Windows.GetAllDocumentWindowsAsync();
			EditorOpen = windows.Any();
			OnEditorOpenChanged();
		}

		private async void DocumentEvents_Opened(string obj)
		{
			EditorOpen = true;
			OnEditorOpenChanged();
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
			var languages = new ReadOnlyCollection<string>(new[] { LanguageUtils.Parse(ext).GetName() });

			var result = await client.GetRecipesForClientSemanticAsync(Term, languages, OnlyPublic, OnlyPrivate, OnlyFavorite, 15, 0);

			Snippets.Clear();

			var settings = EditorSettingsProvider.GetCurrentIndentationSettings();
			var vsSnippets = result.Select(s => SnippetParser.FromCodigaSnippet(s, settings));

			foreach (var snippet in vsSnippets)
			{
				Snippets.Add(snippet);
			}
		}
		#endregion

		#region InsertSnippet command

		public async Task InsertSnippetAsync(object param)
		{
			await HidePreviewAsync(param);
			var snippet = (VisualStudioSnippet)param;

			var client = new ExpansionClient();
			var currentDocView = await VS.Documents.GetActiveDocumentViewAsync();
			client.StartExpansion(currentDocView.TextView, snippet, false);
		}

		#endregion

		#region Preview commands
		public async Task ShowPreviewAsync(object param)
		{
			var snippet = (VisualStudioSnippet)param;
			var currentDocView = await VS.Documents.GetActiveDocumentViewAsync();

			var previewEditor = new CodePreviewSession();
			previewEditor.StartPreviewing(currentDocView.TextView, snippet);
		}

		public async Task HidePreviewAsync(object param)
		{
			var currentDocView = await VS.Documents.GetActiveDocumentViewAsync();
			var previewEditor = new CodePreviewSession();
			previewEditor.StopPreviewing(currentDocView.TextView);
		}
		#endregion

		#region Live search commands
		public async Task OnKeyUp(object param)
		{
			if (Term == null)
				return;

			var args = (KeyEventArgs)param;

			var keyWordCount = Term.Trim().Split(' ').Count();

			if(keyWordCount >= 2 && args.Key == Key.Space)
			{
				await QuerySnippetsAsync(null);
			}
		}
		#endregion

		#region Open Profile command

		public bool IsValidProfile(object param)
		{
			return !string.IsNullOrEmpty(UserName);
		}

		public async Task OpenProfileInBrowser(object param)
		{
			var e = (RequestNavigateEventArgs)param;
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}

		#endregion

		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private void OnEditorOpenChanged()
		{
			GetSnippets.RaiseCanExecuteChanged();
			InsertSnippet.RaiseCanExecuteChanged();
			ShowPreview.RaiseCanExecuteChanged();
			HidePreview.RaiseCanExecuteChanged();
			KeyDown.RaiseCanExecuteChanged();

			if (EditorOpen)
				Watermark = "Search for Snippets";
			else
				Watermark = "Open a file to search for snippets";
		}
	}
}
