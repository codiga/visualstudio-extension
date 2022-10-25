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
		private string _currentLanguage;

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
		public AsyncCommand GetSnippetsCommand { get => _getSnippets; set => _getSnippets = value; }
		public AsyncCommand InsertSnippetCommand { get => _insertSnippet; set => _insertSnippet = value; }
		public AsyncCommand ShowPreviewCommand { get => _showPreview; set => _showPreview = value; }
		public AsyncCommand HidePreviewCommand { get => _hidePreview; set => _hidePreview = value; }
		public AsyncCommand KeyUpCommand { get => _keyDown; set => _keyDown = value; }
		public AsyncCommand OpenProfileCommand { get => _openProfile; set => _openProfile = value; }

		// Search parameters
		public string Term
		{
			get => _term; 
			
			set
			{
				if(_term != value)
				{
					_term = value;
					OnPropertyChanged();
				}
			}
		}
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

		public string CurrentLanguage
		{
			get => _currentLanguage; 
			
			set
			{
				if(_currentLanguage != value)
				{
					_currentLanguage = value;
					OnPropertyChanged();
					ThreadHelper.JoinableTaskFactory.RunAsync(OnLanguageChangedAsync);
				}
			}
		}

		public bool EditorOpen {

			get => _editorOpen;

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
			get => _watermark;

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

			GetSnippetsCommand = new AsyncCommand (QuerySnippetsAsync, IsEditorOpen){ ViewModel = this };
			InsertSnippetCommand = new AsyncCommand (InsertSnippetAsync, IsEditorOpen) { ViewModel = this };
			ShowPreviewCommand = new AsyncCommand (ShowPreviewAsync, IsEditorOpen) { ViewModel = this };
			HidePreviewCommand = new AsyncCommand (HidePreviewAsync, IsEditorOpen) { ViewModel = this };
			KeyUpCommand = new AsyncCommand (OnKeyUp, IsEditorOpen) { ViewModel = this };
			OpenProfileCommand = new AsyncCommand(OpenProfileInBrowser, IsValidProfile) { ViewModel= this };

			VS.Events.DocumentEvents.Opened += DocumentEvents_Opened;
			VS.Events.DocumentEvents.Closed += DocumentEvents_Closed;
			VS.Events.WindowEvents.ActiveFrameChanged += WindowEvents_ActiveFrameChanged;

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

		private void WindowEvents_ActiveFrameChanged(ActiveFrameChangeEventArgs obj)
		{
			var doc = ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				return await obj.NewFrame.GetDocumentViewAsync();
			});

			if (doc == null)
				return;

			var ext = Path.GetExtension(doc.FilePath);
			var lang = LanguageUtils.Parse(ext);
			CurrentLanguage = lang.GetName();
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
			var client = _clientProvider.GetClient();
			var languages = new ReadOnlyCollection<string>(new[] { CurrentLanguage });

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
			GetSnippetsCommand.RaiseCanExecuteChanged();
			InsertSnippetCommand.RaiseCanExecuteChanged();
			ShowPreviewCommand.RaiseCanExecuteChanged();
			HidePreviewCommand.RaiseCanExecuteChanged();
			KeyUpCommand.RaiseCanExecuteChanged();

			if (EditorOpen)
			{
				Watermark = "Search for Snippets";
			}
			else
			{
				Snippets.Clear();
				Watermark = "Open a file to search for snippets";
			}
		}

		private async Task OnLanguageChangedAsync()
		{
			GetSnippetsCommand.RaiseCanExecuteChanged();
			InsertSnippetCommand.RaiseCanExecuteChanged();
			ShowPreviewCommand.RaiseCanExecuteChanged();
			HidePreviewCommand.RaiseCanExecuteChanged();
			KeyUpCommand.RaiseCanExecuteChanged();

			Term = string.Empty;

			await QuerySnippetsAsync(null);
		}
	}
}
