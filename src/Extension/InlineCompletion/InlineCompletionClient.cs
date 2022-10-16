using Community.VisualStudio.Toolkit;
using EnvDTE;
using Extension.AssistantCompletion;
using Extension.SnippetFormats;
using GraphQLClient;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace Extension.InlineCompletion
{
	[Export]
	internal class InlineCompletionClient : IOleCommandTarget
	{
		private IOleCommandTarget _nextCommandHandler;
		private InlineCompletionView _completionView;
		private IWpfTextView _textView;
		private IVsTextView _vsTextView;
		private ListNavigator<VisualStudioSnippet> _snippetNavigator; 
		private CodigaClient _apiClient;
		private ExpansionClient _expansionClient;
		private int triggerIndentationLevel = 0;

		public IReadOnlyRegion CurrentSnippetSpan { get; private set; }
		public SnapshotSpan CurrentInstructtionSpan { get; private set; }

		public InlineCompletionClient()
		{
			_apiClient ??= new CodigaClient();
		}

		public void Initialize(IWpfTextView textView, IVsTextView vsTextView, ExpansionClient expansionClient)
		{
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
			var typedChar = char.MinValue;
			//make sure the input is a char before getting it
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
			{
				typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
			}

			if(CurrentSnippetSpan != null)
			{
				return HandleSessionCommand(nCmdID);
			}

			//_vsTextView.GetBuffer(out var textLines);
			//_vsTextView.GetCaretPos(out var startLine, out var endColumn);
			//textLines.GetLineText(startLine, 0, startLine, endColumn, out var line);

			var caretPos = _textView.Caret.Position.BufferPosition.Position;
			var lineSnapshot = _textView.TextBuffer.CurrentSnapshot.Lines.Single(l => caretPos >= l.Start && caretPos <= l.End);
			var line = lineSnapshot.GetText();


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

			var vssp = VS.GetMefService<SVsServiceProvider>();
			var dte = (_DTE)vssp.GetService(typeof(_DTE));



			_apiClient.GetRecipesForClientSemanticAsync(term, languages, true, 10, 0)
				.ContinueWith(OnQueryFinished);

			using (var edit = _textView.TextBuffer.CreateEdit())
			{
				var settings = EditorSettingsProvider.GetCurrentIndentationSettings();
				triggerIndentationLevel = EditorUtils.GetIndentLevel(line, settings);
				var indent = EditorUtils.GetIndent(triggerIndentationLevel, settings);
				edit.Replace(new Span(caretPos, 1), "\n" + indent);
				edit.Apply();
			}

			caretPos = _textView.Caret.Position.BufferPosition.Position;

			using (var readEdit = _textView.TextBuffer.CreateReadOnlyRegionEdit())
			{
				CurrentSnippetSpan = readEdit.CreateReadOnlyRegion(new Span(caretPos, 1));
				readEdit.Apply();
			}

			CurrentSnippetSpan = InsertSnippetCodePreview(CurrentSnippetSpan, "fetching snippets...");

			//_completionView = new InlineCompletionView(_textView, _settings, null, caretPos);
			// start drawing the adornments
			//_completionView.StartDrawingCompletionView();

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
			_snippetNavigator = new ListNavigator<VisualStudioSnippet>(snippets.ToList());

			var previewCode = SnippetParser.GetPreviewCode(_snippetNavigator.CurrentItem);
			CurrentSnippetSpan = InsertSnippetCodePreview(CurrentSnippetSpan, previewCode);
		}

		private int HandleSessionCommand(uint nCmdID)
		{
			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
			{
				//_completionView.RemoveVisuals();
				//_completionView = null;
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
				//_completionView.RemoveVisuals();
				//_completionView = null;
				RemovePreview(CurrentSnippetSpan);
				CurrentSnippetSpan = null;
				return VSConstants.S_OK;
			}

			if(nCmdID == (uint)VSConstants.VSStd2KCmdID.RIGHT)
			{
				// get next snippet
				var next = _snippetNavigator.Next();
				var i = _snippetNavigator.IndexOf(next);
				var c = _snippetNavigator.Count;

				var previewCode = SnippetParser.GetPreviewCode(next);
				CurrentSnippetSpan = InsertSnippetCodePreview(CurrentSnippetSpan, previewCode);
				//_completionView.UpdateSnippetPreview(next.CodeSnippet.Snippet.Code.CodeString, i + 1, c);

				return VSConstants.S_OK;
			}

			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.LEFT)
			{
				// get previous snippet
				var previous = _snippetNavigator.Previous();
				var i = _snippetNavigator.IndexOf(previous);
				var c = _snippetNavigator.Count;

				var previewCode = SnippetParser.GetPreviewCode(previous); 
				CurrentSnippetSpan = InsertSnippetCodePreview(CurrentSnippetSpan, previewCode);
				//_completionView.UpdateSnippetPreview(previous.CodeSnippet.Snippet.Code.CodeString, i + 1, c);

				return VSConstants.S_OK;
			}

			return VSConstants.S_OK;
		}

		private void RemovePreview(IReadOnlyRegion readOnlyRegion)
		{
			using var readEdit = _textView.TextBuffer.CreateReadOnlyRegionEdit();
			var spanToDelete = readOnlyRegion.Span.GetSpan(_textView.TextBuffer.CurrentSnapshot);
			readEdit.RemoveReadOnlyRegion(readOnlyRegion);
			readEdit.Apply();

			using var edit = _textView.TextBuffer.CreateEdit();
			var newLineSnippet = edit.Delete(spanToDelete);
			var snapshot = edit.Apply();
		}

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
			return EditorUtils.IndentCodeBlock(snippetCode, triggerIndentationLevel, settings);
		}
	}
}
