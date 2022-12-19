using System.IO;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Extension.InlineCompletion
{
    /// <summary>
    /// Utility for working with document and text views.
    /// </summary>
    internal static class DocumentHelper
    {
        /// <summary>
        /// Returns the file extension from the current document.
        /// <br/>
        /// First, it tries to parse <see cref="DocumentView.FilePath"/>, if that is null,
        /// it tries to get the extension via <see cref="ITextBufferExtensions.GetFileName"/>.
        /// </summary>
        /// <param name="documentView"></param>
        /// <param name="textView"></param>
        /// <returns></returns>
        internal static string? GetFileExtension(DocumentView? documentView, IWpfTextView textView)
        {
            if (documentView?.FilePath != null)
                return Path.GetExtension(documentView.FilePath);

            var fileName = ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return textView.TextBuffer.GetFileName();
            });

            return Path.GetExtension(fileName);
        }
    }
}