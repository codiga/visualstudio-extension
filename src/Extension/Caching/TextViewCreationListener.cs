using System.ComponentModel.Composition;
using Extension.Caching;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

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

			// TODO check for language and start polling for each language + dependency
			Cache.StartPolling("Csharp");
		}
	}
}