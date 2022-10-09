using Extension.SnippetFormats;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

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

			textViewAdapter.GetBuffer(out var buffer);
			var type = AdapterService.GetDocumentBuffer(buffer).ContentType;
			var codigaLanguage = CodigaLanguages.Parse(type);
			Cache.StartPolling(codigaLanguage);
		}
	}
}