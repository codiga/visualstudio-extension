using Extension.AssistantCompletion;
using Extension.Caching;
using Extension.SnippetFormats;
using GraphQLClient;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Extension.InlineCompletion
{
	internal class InlineCompletionClient : IOleCommandTarget
	{
		private IOleCommandTarget _nextCommandHandler;
		private InlineCompletionView _completionView;
		private IWpfTextView _textView;
		private IVsTextView _vsTextView;
		private ListNavigator<VisualStudioSnippet> _snippetNavigator; 
		private CodigaClient _apiClient;
		private ExpansionClient _expansionClient;
		private readonly FontSettings _settings;

		public InlineCompletionClient(IWpfTextView textView, IVsTextView vsTextView, ExpansionClient expansionClient, FontSettings settings)
		{
			_textView = textView;
			_vsTextView = vsTextView;
			_apiClient ??= new CodigaClient();
			_expansionClient = expansionClient;
			_settings = settings;
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
			var typedChar = char.MinValue;
			//make sure the input is a char before getting it
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
			{
				typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
			}

			if(_completionView != null)
			{
				return HandleSessionCommand(nCmdID);
			}

			_vsTextView.GetBuffer(out var textLines);
			_vsTextView.GetCaretPos(out var startLine, out var endColumn);
			textLines.GetLineText(startLine, 0, startLine, endColumn, out var line);


			var shouldTriggerCompletion = char.IsWhiteSpace(typedChar) 
				&& EditorUtils.IsSemanticSearchComment(line)
				&& _completionView == null;

			//TODO adjust triggering logic so that only a direct whitespace after search words will trigger
			if (!shouldTriggerCompletion)
			{
				var result = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				return result;
			}

			// start inline completion session
			// query snippets based on keywords
			var term = line.Replace("//", "").Trim();
			var language = CodigaLanguages.Parse(Path.GetExtension(_textView.ToDocumentView().FilePath));
			var languages = new ReadOnlyCollection<string>(new[] { language });

			_apiClient.GetRecipesForClientSemanticAsync(term, languages, true, 10, 0)
				.ContinueWith(OnQueryFinished);

			var caretPos = _textView.Caret.Position.BufferPosition.Position;
			_completionView = new InlineCompletionView(_textView, _settings, null, caretPos);
			// start drawing the adornments
			_completionView.StartDrawingCompletionView();

			return VSConstants.S_OK;
		}

		private int CommitCurrentSnippet()
		{
			_expansionClient.StartExpansion(_vsTextView, _snippetNavigator.CurrentItem);
			return VSConstants.S_OK;
		}

		private async Task OnQueryFinished(Task<IReadOnlyCollection<CodigaSnippet>> result)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			var setting = EditorSettingsProvider.GetCurrentIndentationSettings();
			var snippets = result.Result.Select(s => SnippetParser.FromCodigaSnippet(s, setting));
			// get snippet code
			_snippetNavigator = new ListNavigator<VisualStudioSnippet>(snippets.ToList());
			_completionView.UpdateSnippetPreview(_snippetNavigator.First().CodeSnippet.Snippet.Code.CodeString, 1, _snippetNavigator.Count);
		}

		private int HandleSessionCommand(uint nCmdID)
		{
			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
			{
				_completionView.RemoveVisuals();
				_completionView = null;
				CommitCurrentSnippet();
				return VSConstants.S_OK;
			}

			if(nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
			{
				_completionView.RemoveVisuals();
				_completionView = null;
				return VSConstants.S_OK;
			}

			if(nCmdID == (uint)VSConstants.VSStd2KCmdID.RIGHT)
			{
				// get next snippet
				var next = _snippetNavigator.Next();
				var i = _snippetNavigator.IndexOf(next);
				var c = _snippetNavigator.Count;
				_completionView.UpdateSnippetPreview(next.CodeSnippet.Snippet.Code.CodeString, i + 1, c);
				return VSConstants.S_OK;
			}

			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.LEFT)
			{
				// get previous snippet
				var previous = _snippetNavigator.Previous();
				var i = _snippetNavigator.IndexOf(previous);
				var c = _snippetNavigator.Count;
				_completionView.UpdateSnippetPreview(previous.CodeSnippet.Snippet.Code.CodeString, i + 1, c);
				return VSConstants.S_OK;
			}

			return VSConstants.S_OK;
		}
	}
}
