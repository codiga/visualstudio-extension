using Community.VisualStudio.Toolkit;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace Extension.Caching
{
	[Export(typeof(IVsTextViewCreationListener))]
	[Name("text view creation listener")]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	public class TextViewCreationListener : IVsTextViewCreationListener
	{
		[Import]
		internal IVsEditorAdaptersFactoryService AdapterService = null;

		[Import]
		internal SnippetCache Cache;

		public void VsTextViewCreated(IVsTextView textViewAdapter)
		{
			ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
			if (textView == null)
				return;
			textView.Closed += TextView_Closed;
			textView.TextBuffer.Changed += TextBuffer_Changed;
			var path = VS.Documents.GetActiveDocumentViewAsync().GetAwaiter().GetResult().FilePath;
			var ext = Path.GetExtension(path);
			var codigaLanguage = CodigaLanguages.Parse(ext);
			Cache.StartPolling(codigaLanguage);
		}

		private void TextBuffer_Changed(object sender, Microsoft.VisualStudio.Text.TextContentChangedEventArgs e)
		{
			var buffer = (ITextBuffer)sender;
			var path = VS.Documents.GetActiveDocumentViewAsync().GetAwaiter().GetResult().FilePath;
			var ext = Path.GetExtension(path);
			var codigaLanguage = CodigaLanguages.Parse(ext);
			Cache.ReportActivity(codigaLanguage);
		}

		private void TextView_Closed(object sender, System.EventArgs e)
		{
			var textView = (ITextView)sender;
			var type = textView.TextBuffer.ContentType;
			var path = VS.Documents.GetActiveDocumentViewAsync().GetAwaiter().GetResult().FilePath;
			var ext = Path.GetExtension(path);
			var codigaLanguage = CodigaLanguages.Parse(ext);
			Cache.StopPolling(codigaLanguage);
			textView.TextBuffer.Changed -= TextBuffer_Changed;
			textView.Closed -= TextView_Closed;
		}
	}
}