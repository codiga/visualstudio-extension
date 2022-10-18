using Extension.SnippetFormats;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension.InlineCompletion.Preview
{
	internal static class PreviewTextEditor
	{
		/// <summary>
		/// Inserts the provided coded at the given span by replacing the previous read-only region.
		/// </summary>
		/// <param name="textView"></param>
		/// <param name="readOnlyRegion"></param>
		/// <param name="snippetCode"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		public static IReadOnlyRegion InsertSnippetCodePreview(ITextView textView, IReadOnlyRegion readOnlyRegion, string snippetCode, int position)
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

		public static int PrepareInsertion(ITextView view, string triggeringLine, int caretPos)
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
			return EditorUtils.IndentCodeBlock(snippetCode, indentLevel, settings);
		}
	}
}
