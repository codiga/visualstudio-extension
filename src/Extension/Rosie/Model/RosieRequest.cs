using System.Collections.Generic;

namespace Extension.Rosie.Model
{
    /// <summary>
    /// Rosie request object sent to the Codiga API.
    /// </summary>
    public class RosieRequest
    {
        public string Filename { get; }

        /// <summary>
        /// The Rosie language string.
        /// </summary>
        /// <seealso cref="RosieUtils#getRosieLanguage(io.codiga.api.type.LanguageEnumeration)"/>
        /// <seealso cref="RosieClient"/>
        public string Language { get; }

        public string FileEncoding { get; }

        /// <summary>
        /// The base64-encoded version of the code to be analysed.
        /// </summary>
        public string CodeBase64 { get; }

        public IReadOnlyList<RosieRule>? Rules { get; }
        public bool LogOutput { get; }

        public RosieRequest(string filename, string language, string fileEncoding, string codeBase64,
            IReadOnlyList<RosieRule>? rules, bool logOutput)
        {
            Filename = filename;
            Language = language;
            FileEncoding = fileEncoding;
            CodeBase64 = codeBase64;
            Rules = rules;
            LogOutput = logOutput;
        }
    }
}