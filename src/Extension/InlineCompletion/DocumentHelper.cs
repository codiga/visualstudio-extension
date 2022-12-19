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
        /// It has the following fallback logic:
        /// <ul>
        ///     <li>First, it tries to parse <see cref="DocumentView.FilePath"/>,</li>
        ///     <li>then the underlying <see cref="ITextDocument.FilePath"/>,</li>
        ///     <li>then via the underlying <c>DocumentView.TextBuffer.GetFileName</c>,</li>
        ///     <li>finally, it tries to get the extension via <see cref="ITextBufferExtensions.GetFileName"/></li>
        /// </ul>
        /// </summary>
        /// <param name="documentView">The document view</param>
        /// <param name="textView">The text view</param>
        /// <returns>The file extension, or null if the extension could not be retrieved.</returns>
        internal static string? GetFileExtension(DocumentView? documentView, IWpfTextView? textView = null)
        {
            if (documentView?.FilePath != null)
                return Path.GetExtension(documentView.FilePath);

            if (documentView?.Document?.FilePath != null)
                return Path.GetExtension(documentView.Document.FilePath);

            if (documentView?.TextBuffer != null)
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return Path.GetExtension(documentView?.TextBuffer?.GetFileName());
                });

            if (textView != null)
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return Path.GetExtension(textView.TextBuffer.GetFileName());
                });
            }

            return null;
        }
    }
}