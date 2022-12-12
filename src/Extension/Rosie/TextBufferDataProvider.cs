using System;
using Microsoft.VisualStudio.Text;

namespace Extension.Rosie
{
    /// <summary>
    /// Provides a way to 'mock' the behaviour of an <see cref="ITextBuffer"/> and the response from Codiga.
    /// <br/>
    /// <c>ITextBuffer.GetFileName()</c> must be called on the main/UI thread, but
    /// in case of unit testing, there is no such thing, thus bypassing it with this class.
    /// </summary>
    public class TextBufferDataProvider
    {
        public Func<ITextBuffer, string?> FileName { get; set; } = buffer => buffer.GetFileName();

        public Func<ITextBuffer, string> FileText { get; set; } = buffer => buffer.CurrentSnapshot.GetText();

        /// <summary>
        /// To limit code execution when something is not achievable, or unnecessary to call during unit testing.  
        /// </summary>
        public bool IsTestMode { get; set; }
    }
}