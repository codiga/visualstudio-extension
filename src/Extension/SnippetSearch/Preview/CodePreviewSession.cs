using System.Linq;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Extension.SnippetSearch.Preview
{
	public interface ICodePreviewSession
	{
		/// <summary>
		/// Starts a new preview session by inserting the snippet code at the current caret position.
		/// </summary>
		/// <param name="textView"></param>
		/// <param name="code"></param>
		public void StartPreviewing(IWpfTextView textView, VisualStudioSnippet code);

		/// <summary>
		/// Stops the current preview by removing the previe code from the editor.
		/// </summary>
		/// <param name="textView"></param>
		public void StopPreviewing(IWpfTextView textView);
	}


	internal class CodePreviewSession : ICodePreviewSession
	{
		/// <summary>
		/// The current code block in preview. Needs to be statically available for the classifier.
		/// </summary>
		public static IReadOnlyRegion CurrentPreview { get; private set; } = null;

		public void StartPreviewing(IWpfTextView textView, VisualStudioSnippet snippet)
		{
			if (CurrentPreview != null)
				return;

			var caret = textView.Caret;
			var caretBufferPosition = caret.Position.BufferPosition.Position;

			var previewCode = SnippetParser.GetPreviewCode(snippet);
			var settings = EditorSettingsProvider.GetCurrentIndentationSettings();

			var indentedCode = EditorUtils.IndentCodeBlock(previewCode, caret, settings);

			ITextSnapshot currentSnapshot;
			using (var edit = textView.TextBuffer.CreateEdit())
			{
				var insertPosition = caretBufferPosition;

				// if non-virtual indented line we need to insert at the beginning of the line
				var currentLine = textView.TextSnapshot.GetLineFromPosition(caret.Position.BufferPosition.Position);
				if (currentLine.GetText().All(c => c == ' ' || c == '\t'))
					insertPosition = currentLine.Start.Position;
				
				edit.Insert(insertPosition, indentedCode);
				currentSnapshot = edit.Apply();
			}

			var newSpan = new Span(caretBufferPosition, indentedCode.Length);

			// create read only region for the new snippet
			IReadOnlyRegion newRegion;
			using (var readEdit = textView.TextBuffer.CreateReadOnlyRegionEdit())
			{
				newRegion = readEdit.CreateReadOnlyRegion(newSpan);

				readEdit.Apply();
			}

			CurrentPreview = newRegion;
		}

		public void StopPreviewing(IWpfTextView textView)
		{
			if (CurrentPreview == null)
				return;

			using var readEdit = textView.TextBuffer.CreateReadOnlyRegionEdit();
			var spanToDelete = CurrentPreview.Span.GetSpan(textView.TextBuffer.CurrentSnapshot);
			readEdit.RemoveReadOnlyRegion(CurrentPreview);
			readEdit.Apply();

			using var edit = textView.TextBuffer.CreateEdit();
			edit.Delete(spanToDelete);
			edit.Apply();

			CurrentPreview = null;
		}

		/// <summary>
		/// Inserts the provided coded at the given span by replacing the previous read-only region.
		/// </summary>
		/// <param name="textView"></param>
		/// <param name="readOnlyRegion"></param>
		/// <param name="snippetCode"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		private static IReadOnlyRegion InsertSnippetCodePreview(ITextView textView, IReadOnlyRegion readOnlyRegion, string snippetCode, int position)
		{

			// remove current read only region
			ITextSnapshot currentSnapshot;
			using (var readEdit = textView.TextBuffer.CreateReadOnlyRegionEdit())
			{
				readEdit.RemoveReadOnlyRegion(readOnlyRegion);
				currentSnapshot = readEdit.Apply();
			}

			var spanToReplace = readOnlyRegion.Span.GetSpan(textView.TextBuffer.CurrentSnapshot);
			// replace snippet with new snippet

			using (var edit = textView.TextBuffer.CreateEdit())
			{
				edit.Replace(spanToReplace, snippetCode);
				currentSnapshot = edit.Apply();
			}
			var caretPosition = new SnapshotPoint(textView.TextSnapshot, position);
			textView.Caret.MoveTo(caretPosition);

			var newSpan = new Span(spanToReplace.Start, snippetCode.Length);
			var snapSpan = new SnapshotSpan(currentSnapshot, newSpan);

			// create read only region for the new snippet
			IReadOnlyRegion newRegion;
			using (var readEdit = textView.TextBuffer.CreateReadOnlyRegionEdit())
			{
				newRegion = readEdit.CreateReadOnlyRegion(newSpan);

				readEdit.Apply();
			}

			return newRegion;
		}

		private static int PrepareInsertion(ITextView view, string triggeringLine, int caretPos)
		{
			using var edit = view.TextBuffer.CreateEdit();
			var indentationSettings = EditorSettingsProvider.GetCurrentIndentationSettings();
			var indentLevel = EditorUtils.GetIndentLevel(triggeringLine, indentationSettings);
			var indent = EditorUtils.GetIndent(indentLevel, indentationSettings);
			edit.Replace(new Span(caretPos, 1), "\n" + indent);
			edit.Apply();
			return indentLevel;
		}

		/// <summary>
		/// Remove the code preview from the editor.
		/// </summary>
		/// <param name="readOnlyRegion"></param>
		private static void RemovePreview(ITextView view, IReadOnlyRegion readOnlyRegion)
		{
			if (readOnlyRegion == null)
				return;

			using var readEdit = view.TextBuffer.CreateReadOnlyRegionEdit();
			var spanToDelete = readOnlyRegion.Span.GetSpan(view.TextBuffer.CurrentSnapshot);
			readEdit.RemoveReadOnlyRegion(readOnlyRegion);
			readEdit.Apply();

			using var edit = view.TextBuffer.CreateEdit();
			var newLineSnippet = edit.Delete(spanToDelete);
			var snapshot = edit.Apply();
		}

		private static string FormatSnippet(string snippetCode, int indentLevel)
		{
			var settings = EditorSettingsProvider.GetCurrentIndentationSettings();
			return EditorUtils.IndentCodeBlock(snippetCode, indentLevel, settings, false);
		}
	}
}
