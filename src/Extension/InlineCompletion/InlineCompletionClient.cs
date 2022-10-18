using Extension.AssistantCompletion;
using Extension.Caching;
using Extension.SnippetFormats;
using GraphQLClient;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Extension.InlineCompletion
{
	/// <summary>
	/// This class is responsible for the inline completion session lifecycle.
	/// The main purpose is to provide the user with a good preview of potential snippets returned by the semantic search.
	/// The snippet insertion process is passed to the <see cref="ExpansionClient"/>
	/// </summary>
	[Export]
	internal class InlineCompletionClient : IOleCommandTarget
	{
		private IOleCommandTarget _nextCommandHandler;
		private IWpfTextView _textView;
		private IVsTextView _vsTextView;

		private InlineCompletionInstructionsView _instructionsView;
		private ListNavigator<VisualStudioSnippet> _snippetNavigator; 

		private CodigaClientProvider _clientProvider;
		private ExpansionClient _expansionClient;

		private int _triggerCaretPosition = 0;

		/// <summary>
		/// Initialize the client and start listening for commands
		/// </summary>
		/// <param name="textView"></param>
		/// <param name="vsTextView"></param>
		/// <param name="expansionClient"></param>
		/// <param name="clientProvider"></param>
		public void Initialize(IWpfTextView textView, IVsTextView vsTextView, ExpansionClient expansionClient, CodigaClientProvider clientProvider)
		{
			_clientProvider = clientProvider;
			_textView = textView;
			_vsTextView = vsTextView;
			_expansionClient = expansionClient;
			vsTextView.AddCommandFilter(this, out _nextCommandHandler);
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}

		/// <summary>
		/// This handles incoming commands during the inline completion session.
		/// Commands are NextSnippet(tbd), PreviousSnippet(tbd), Commit [Tab], Cancel [ESC]
		/// </summary>
		/// <param name="pguidCmdGroup"></param>
		/// <param name="nCmdID"></param>
		/// <param name="nCmdexecopt"></param>
		/// <param name="pvaIn"></param>
		/// <param name="pvaOut"></param>
		/// <returns></returns>
		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			var codigaSettings = EditorSettingsProvider.GetCurrentCodigaSettings();

			if (!codigaSettings.UseInlineCompletion)
			{
				return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

			var typedChar = char.MinValue;
			//make sure the input is a char before getting it
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
			{
				typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
			}

			if(_instructionsView != null)
			{
				return HandleSessionCommand(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

			var caretPos = _textView.Caret.Position.BufferPosition.Position;
			var lineSnapshot = _textView.TextBuffer.CurrentSnapshot.Lines.Single(l => caretPos >= l.Start && caretPos <= l.End);
			var triggeringLine = lineSnapshot.GetText();


			var shouldTriggerCompletion = char.IsWhiteSpace(typedChar) 
				&& EditorUtils.IsSemanticSearchComment(triggeringLine)
				&& _instructionsView == null;

			//TODO adjust triggering logic so that only a direct whitespace after search words will trigger
			if (!shouldTriggerCompletion)
			{
				return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

			_triggerCaretPosition = caretPos;

			// start inline completion session

			// query snippets based on keywords
			var term = triggeringLine.Replace("//", "").Trim();
			var language = CodigaLanguages.Parse(Path.GetExtension(_textView.ToDocumentView().FilePath));
			var languages = new ReadOnlyCollection<string>(new[] { language });

			var client = _clientProvider.GetClient();
			client.GetRecipesForClientSemanticAsync(term, languages, false, 10, 0)
				.ContinueWith(OnQueryFinished);

			_instructionsView = new InlineCompletionInstructionsView(_textView, _triggerCaretPosition);

			// start drawing the adornments for the instructions
			_instructionsView.StartDrawingInstructions();

			return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
		}

		/// <summary>
		/// Calls the ExpansionClient to insert the selected snippet.
		/// </summary>
		/// <returns></returns>
		private int CommitCurrentSnippet()
		{
			_expansionClient.StartExpansion(_vsTextView, _snippetNavigator.CurrentItem, _clientProvider);
			return VSConstants.S_OK;
		}

		/// <summary>
		/// Callback for when the API query returns with its results.
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		private async Task OnQueryFinished(Task<System.Collections.Generic.IReadOnlyCollection<CodigaSnippet>> result)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			var setting = EditorSettingsProvider.GetCurrentIndentationSettings();
			var snippets = result.Result.Select(s => SnippetParser.FromCodigaSnippet(s, setting));
			var currentIndex = 0;

			if (_instructionsView == null)
				return;

			if (snippets.Any())
			{
				_snippetNavigator = new ListNavigator<VisualStudioSnippet>(snippets.ToList());
				var previewCode = SnippetParser.GetPreviewCode(_snippetNavigator.CurrentItem);
				currentIndex = _snippetNavigator.IndexOf(_snippetNavigator.CurrentItem) + 1;
				_instructionsView.UpdateInstructions(previewCode, currentIndex, _snippetNavigator.Count);
			}
			else
			{
				_instructionsView.ShowPreview = false;
				_instructionsView.UpdateInstructions(null, 0, 0);
			}
		}

		/// <summary>
		/// Handles all the commands during an open inline session.
		/// </summary>
		/// <param name="nCmdID"></param>
		/// <returns></returns>
		private int HandleSessionCommand(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
			{
				_instructionsView.RemoveInstructions();
				_instructionsView = null;
				CommitCurrentSnippet();
				return VSConstants.S_OK;
			}

			else if(nCmdID == (uint)VSConstants.VSStd2KCmdID.RIGHT)
			{
				if(_snippetNavigator == null || _snippetNavigator.Count == 0)
					return VSConstants.S_OK;

				// get next snippet
				var next = _snippetNavigator.Next();
				var i = _snippetNavigator.IndexOf(next);
				var c = _snippetNavigator.Count;

				var previewCode = SnippetParser.GetPreviewCode(next);
				_instructionsView.UpdateInstructions(previewCode, i + 1, c);

				return VSConstants.S_OK;
			}

			else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.LEFT)
			{
				if (_snippetNavigator == null || _snippetNavigator.Count == 0)
					return VSConstants.S_OK;

				// get previous snippet
				var previous = _snippetNavigator.Previous();
				var i = _snippetNavigator.IndexOf(previous);
				var c = _snippetNavigator.Count;

				var previewCode = SnippetParser.GetPreviewCode(previous); 
				_instructionsView.UpdateInstructions(previewCode, i + 1, c);

				return VSConstants.S_OK;
			}
			else
			{
				_instructionsView.RemoveInstructions();
				_instructionsView = null;

				var result = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				return result;
			}
		}
	}
}
