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
using System.Diagnostics;
using System.Windows.Navigation;
using System;
using Extension.Logging;
using GraphQLClient;

namespace Extension.SearchWindow.View
{
	/// <summary>
	/// View model representing the view logic of the search window.
	/// </summary>
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
		/// <summary>
		/// The command for executing a snippet search.
		/// </summary>
		public AsyncCommand GetSnippetsCommand { get => _getSnippets; set => _getSnippets = value; }

		/// <summary>
		/// Command to insert the snippet into the editor.
		/// </summary>
		public AsyncCommand InsertSnippetCommand { get => _insertSnippet; set => _insertSnippet = value; }

		/// <summary>
		/// Command to show a preview of a snippet.
		/// </summary>
		public AsyncCommand ShowPreviewCommand { get => _showPreview; set => _showPreview = value; }

		/// <summary>
		/// Command to remove the preview from the editor.
		/// </summary>
		public AsyncCommand HidePreviewCommand { get => _hidePreview; set => _hidePreview = value; }

		/// <summary>
		/// Key up command used to support live search in the search box.
		/// </summary>
		public AsyncCommand KeyUpCommand { get => _keyDown; set => _keyDown = value; }

		/// <summary>
		/// Command for opening the profile page on click on the link.
		/// </summary>
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

		public bool ValidEditor => EditorOpen && CurrentLanguage != "Unknown";

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

			if (_clientProvider.TryGetClient(out var client))
			{
				var result = ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					return await client.GetUserAsync();
				});

				if(result.Errors == null || !result.Errors.Any())
					UserName = result.Data?.User?.UserName ?? "";
			}

			EditorOpen = windows.Any();

			if (EditorOpen)
			{
				var doc = ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					return await VS.Documents.GetActiveDocumentViewAsync();
				});

				var ext = Path.GetExtension(doc.FilePath);
				var lang = LanguageUtils.Parse(ext);
				CurrentLanguage = lang.GetName();
			}

			AllSnippets = true;

			RefreshWatermark();
		}

		/// <summary>
		/// Triggered when switching between open editors. 
		/// We update the search window based on the language of the opened file.
		/// </summary>
		/// <param name="obj"></param>
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

		/// <summary>
		/// Triggered when closing an editor.
		/// We check here if all editors are closed.
		/// </summary>
		/// <param name="obj"></param>
		private async void DocumentEvents_Closed(string obj)
		{
			var windows = await VS.Windows.GetAllDocumentWindowsAsync();
			EditorOpen = windows.Any();
			OnEditorOpenChanged();
		}

		/// <summary>
		/// Update if an editor was opened.
		/// </summary>
		/// <param name="obj"></param>
		private async void DocumentEvents_Opened(string obj)
		{
			EditorOpen = true;
			OnEditorOpenChanged();
		}

		#region GetSnippets command

		/// <summary>
		/// Used as the CanExecute predicate for the commands.
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		public bool IsEditorOpen(object param)
		{
			return ValidEditor;
		}

		/// <summary>
		/// Queries snippets from the GraphQL API and puts them in the ObservableCollection.
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		public async Task QuerySnippetsAsync(object param)
		{
			if (!_clientProvider.TryGetClient(out var client))
				return;

			var languages = new ReadOnlyCollection<string>(new[] { CurrentLanguage });

			try
			{
				var result = await client.GetRecipesForClientSemanticAsync(Term, languages, OnlyPublic, OnlyPrivate, OnlyFavorite, 15, 0);

				Snippets.Clear();

				var settings = EditorSettingsProvider.GetCurrentIndentationSettings();
				var vsSnippets = result.Select(s => SnippetParser.FromCodigaSnippet(s, settings));

				foreach (var snippet in vsSnippets)
				{
					Snippets.Add(snippet);
				}
			}
			catch (CodigaAPIException e)
			{
				ExtensionLogger.LogException(e);
			}
		}
		#endregion

		#region InsertSnippet command

		/// <summary>
		/// Insert the snippet using the ExpansionClient
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		public async Task InsertSnippetAsync(object param)
		{
			await HidePreviewAsync(param);
			var snippet = (VisualStudioSnippet)param;

			var client = new ExpansionClient();
			var currentDocView = await VS.Documents.GetActiveDocumentViewAsync();
			await currentDocView.WindowFrame.ShowAsync();

			try 
			{
				client.StartExpansion(currentDocView.TextView, snippet, false);
			}
			catch (Exception e)
			{
				ExtensionLogger.LogException(e);
			}
		}

		#endregion

		#region Preview commands

		/// <summary>
		/// Show preview by starting a new CodePreviewSession.
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		public async Task ShowPreviewAsync(object param)
		{
			try
			{
				var snippet = (VisualStudioSnippet)param;
				var currentDocView = await VS.Documents.GetActiveDocumentViewAsync();

				var previewEditor = new CodePreviewSession();
				previewEditor.StartPreviewing(currentDocView.TextView, snippet);
			}
			catch(Exception e)
			{
				ExtensionLogger.LogException(e);
			}
		}

		/// <summary>
		/// Remove the preview from the editor.
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		public async Task HidePreviewAsync(object param)
		{
			try
			{
				var currentDocView = await VS.Documents.GetActiveDocumentViewAsync();
				var previewEditor = new CodePreviewSession();
				previewEditor.StopPreviewing(currentDocView.TextView);
			}
			catch(Exception e)
			{
				ExtensionLogger.LogException(e);
			}
		}
		#endregion

		#region Live search commands
		/// <summary>
		/// Perform a search after two keywords were typed.
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Used to inform the view of changed properties so that the bound values are refreshed.
		/// </summary>
		/// <param name="name"></param>
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

			OnPropertyChanged(nameof(ValidEditor));
			RefreshWatermark();

			if (!ValidEditor)
			{
				Snippets.Clear();
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

			OnPropertyChanged(nameof(ValidEditor));
			RefreshWatermark();

			if (ValidEditor)
			{
				await QuerySnippetsAsync(null);
			}
			else
			{
				Snippets.Clear();
			}
		}

		private void RefreshWatermark()
		{
			if(ValidEditor)
				Watermark = "Search for Snippets";
			else if(EditorOpen && !ValidEditor)
				Watermark = "Language not supported";
			else
				Watermark = "Open a file to search for snippets";
		}
	}
}
