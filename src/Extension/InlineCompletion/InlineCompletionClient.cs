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

		private int _triggerIndentationLevel = 0;
		private int _triggerCaretPosition = 0;
		private int _insertionPosition = 0;
		public IReadOnlyRegion CurrentSnippetSpan { get; private set; }

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
				var result = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				return result;
			}

			var typedChar = char.MinValue;
			//make sure the input is a char before getting it
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
			{
				typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
			}

			if(_instructionsView != null)
			{
				return HandleSessionCommand(nCmdID);
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
				var result = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				return result;
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

			// set up insertion position below the triggering commment
			using (var edit = _textView.TextBuffer.CreateEdit())
			{
				var indentationSettings = EditorSettingsProvider.GetCurrentIndentationSettings();
				_triggerIndentationLevel = EditorUtils.GetIndentLevel(triggeringLine, indentationSettings);
				var indent = EditorUtils.GetIndent(_triggerIndentationLevel, indentationSettings);
				edit.Replace(new Span(caretPos, 1), "\n" + indent);
				edit.Apply();
			}

			_insertionPosition = _textView.Caret.Position.BufferPosition.Position;

			using (var readEdit = _textView.TextBuffer.CreateReadOnlyRegionEdit())
			{
				CurrentSnippetSpan = readEdit.CreateReadOnlyRegion(new Span(_insertionPosition, 1));
				readEdit.Apply();
			}

			CurrentSnippetSpan = InsertSnippetCodePreview(CurrentSnippetSpan, "fetching snippets...");

			_instructionsView = new InlineCompletionInstructionsView(_textView, _triggerCaretPosition);

			// start drawing the adornments for the instructions
			_instructionsView.StartDrawingInstructions();

			return VSConstants.S_OK;
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

			if (snippets.Any())
			{
				_snippetNavigator = new ListNavigator<VisualStudioSnippet>(snippets.ToList());
				var previewCode = SnippetParser.GetPreviewCode(_snippetNavigator.CurrentItem);
				CurrentSnippetSpan = InsertSnippetCodePreview(CurrentSnippetSpan, previewCode);
				currentIndex = _snippetNavigator.IndexOf(_snippetNavigator.CurrentItem) + 1;
				_instructionsView.UpdateInstructions(currentIndex, _snippetNavigator.Count);
			}
			else
			{
				RemovePreview(CurrentSnippetSpan);
				CurrentSnippetSpan = null;
			}
		}

		/// <summary>
		/// Handles all the commands during an open inline session.
		/// </summary>
		/// <param name="nCmdID"></param>
		/// <returns></returns>
		private int HandleSessionCommand(uint nCmdID)
		{
			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
			{
				_instructionsView.RemoveInstructions();
				_instructionsView = null;
				var start = CurrentSnippetSpan.Span.GetStartPoint(_textView.TextBuffer.CurrentSnapshot);
				RemovePreview(CurrentSnippetSpan);
				var startPosition = new SnapshotSpan(_textView.TextBuffer.CurrentSnapshot, new Span(start.Position, 0));
				_textView.Selection.Select(startPosition, false);
				CurrentSnippetSpan = null;
				CommitCurrentSnippet();
				return VSConstants.S_OK;
			}

			if(nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
			{
				_instructionsView.RemoveInstructions();
				_instructionsView = null;
				RemovePreview(CurrentSnippetSpan);
				CurrentSnippetSpan = null;
				return VSConstants.S_OK;
			}

			if(nCmdID == (uint)VSConstants.VSStd2KCmdID.RIGHT)
			{
				if(_snippetNavigator == null || _snippetNavigator.Count == 0)
					return VSConstants.S_OK;

				// get next snippet
				var next = _snippetNavigator.Next();
				var i = _snippetNavigator.IndexOf(next);
				var c = _snippetNavigator.Count;

				var previewCode = SnippetParser.GetPreviewCode(next);
				CurrentSnippetSpan = InsertSnippetCodePreview(CurrentSnippetSpan, previewCode);
				_instructionsView.UpdateInstructions(i + 1, c);

				return VSConstants.S_OK;
			}

			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.LEFT)
			{
				if (_snippetNavigator == null || _snippetNavigator.Count == 0)
					return VSConstants.S_OK;

				// get previous snippet
				var previous = _snippetNavigator.Previous();
				var i = _snippetNavigator.IndexOf(previous);
				var c = _snippetNavigator.Count;

				var previewCode = SnippetParser.GetPreviewCode(previous); 
				CurrentSnippetSpan = InsertSnippetCodePreview(CurrentSnippetSpan, previewCode);
				_instructionsView.UpdateInstructions(i + 1, c);

				return VSConstants.S_OK;
			}

			return VSConstants.S_OK;
		}

		/// <summary>
		/// Remove the code preview from the editor.
		/// </summary>
		/// <param name="readOnlyRegion"></param>
		private void RemovePreview(IReadOnlyRegion readOnlyRegion)
		{
			if (readOnlyRegion == null)
				return;

			using var readEdit = _textView.TextBuffer.CreateReadOnlyRegionEdit();
			var spanToDelete = readOnlyRegion.Span.GetSpan(_textView.TextBuffer.CurrentSnapshot);
			readEdit.RemoveReadOnlyRegion(readOnlyRegion);
			readEdit.Apply();

			using var edit = _textView.TextBuffer.CreateEdit();
			var newLineSnippet = edit.Delete(spanToDelete);
			var snapshot = edit.Apply();
		}

		/// <summary>
		/// Inserts the provided coded at the given span by replacing the previous read-only region.
		/// </summary>
		/// <param name="readOnlyRegion"></param>
		/// <param name="snippetCode"></param>
		/// <returns></returns>
		private IReadOnlyRegion InsertSnippetCodePreview(IReadOnlyRegion readOnlyRegion, string snippetCode)
		{
			var indentedCode = FormatSnippet(snippetCode);

			// remove current read only region
			ITextSnapshot currentSnapshot;
			using (var readEdit = _textView.TextBuffer.CreateReadOnlyRegionEdit())
			{
				readEdit.RemoveReadOnlyRegion(readOnlyRegion);
				currentSnapshot = readEdit.Apply();
			}

			var spanToReplace = readOnlyRegion.Span.GetSpan(_textView.TextBuffer.CurrentSnapshot);
			// replace snippet with new snippet

			using (var edit = _textView.TextBuffer.CreateEdit())
			{
				edit.Replace(spanToReplace, indentedCode);
				currentSnapshot = edit.Apply();
			}
			var caretPosition = new SnapshotPoint(_textView.TextSnapshot, _triggerCaretPosition);
			_textView.Caret.MoveTo(caretPosition);

			var newSpan = new Span(spanToReplace.Start, indentedCode.Length);
			var snapSpan = new SnapshotSpan(currentSnapshot, newSpan);
			
			// create read only region for the new snippet
			IReadOnlyRegion newRegion;
			using (var readEdit = _textView.TextBuffer.CreateReadOnlyRegionEdit())
			{
				newRegion = readEdit.CreateReadOnlyRegion(newSpan);

				readEdit.Apply();
			}
			
			return newRegion;
		}

		private string FormatSnippet(string snippetCode)
		{
			var settings = EditorSettingsProvider.GetCurrentIndentationSettings();
			return EditorUtils.IndentCodeBlock(snippetCode, _triggerIndentationLevel, settings);
		}
	}
}
