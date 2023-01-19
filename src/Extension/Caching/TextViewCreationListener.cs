using Community.VisualStudio.Toolkit;
using Extension.Logging;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.IO;

namespace Extension.Caching
{
	/// <summary>
	/// Starts polling session on opened editors.
	/// </summary>
	[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	internal sealed class TextViewCreationListener : IWpfTextViewCreationListener
	{
		[Import]
		internal IVsEditorAdaptersFactoryService AdapterService;

		[Import]
		internal SnippetCache Cache;

		/// <summary>
		/// Called when a text view having matching roles is created over a text data model having a matching content type.
		/// Instantiates a TextAdornment1 manager when the textView is created.
		/// </summary>
		/// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
		public void TextViewCreated(IWpfTextView textView)
		{
			var doc = EditorUtils.ToDocumentView(textView);

			if (doc == null)
				return;

			try
			{
				var ext = Path.GetExtension(doc.FilePath);
				var codigaLanguage = LanguageUtils.Parse(ext);

				if (codigaLanguage == LanguageUtils.LanguageEnumeration.Unknown)
					return;

				textView.Closed += TextView_Closed;
				textView.TextBuffer.Changed += TextBuffer_Changed;

				Cache.StartPolling(codigaLanguage);
			}
			catch (Exception e)
			{
				ExtensionLogger.LogException(e);
			}
		}

		private void TextView_Closed(object sender, System.EventArgs e)
		{
			var textView = (ITextView)sender;
			var type = textView.TextBuffer.ContentType;

			var doc = ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				return await VS.Documents.GetActiveDocumentViewAsync();
			});

			var ext = Path.GetExtension(doc.FilePath);
			var codigaLanguage = LanguageUtils.Parse(ext);
			Cache.StopPolling(codigaLanguage);
			textView.TextBuffer.Changed -= TextBuffer_Changed;
			textView.Closed -= TextView_Closed;
		}

		private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
		{
			var buffer = (ITextBuffer)sender;
			var path = buffer.GetTextDocument().FilePath;
			var ext = Path.GetExtension(path);
			var codigaLanguage = LanguageUtils.Parse(ext);
			Cache.ReportActivity(codigaLanguage);
		}
	}
}
