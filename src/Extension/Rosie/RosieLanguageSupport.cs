using System.Collections.Generic;
using static Extension.SnippetFormats.LanguageUtils;

namespace Extension.Rosie
{
    /// <summary>
    /// Utility for the Rosie integration.
    /// </summary>
    public static class RosieLanguageSupport
    {
        /// <summary>
        /// Languages currently supported by Rosie.
        /// </summary>
        private static readonly ISet<LanguageEnumeration> SupportedLanguages =
            new HashSet<LanguageEnumeration>
            {
                LanguageEnumeration.Python,
                LanguageEnumeration.Javascript,
                LanguageEnumeration.Typescript
            };
        
        /// <summary>
        /// Returns whether the argument language is supported by Rosie.
        /// </summary>
        /// <param name="language">the language to check</param>
        /// <returns></returns>
        internal static bool IsLanguageSupported(LanguageEnumeration language)
        {
            return SupportedLanguages.Contains(language);
        }
        
        /// <summary>
        /// Returns whether the language of the provided filename is supported by Rosie.
        /// </summary>
        /// <param name="fileName">The file name to validate the language of.</param>
        /// <returns>True if the file language is supported, false otherwise.</returns>
        internal static bool IsLanguageOfFileSupported(string? fileName)
        {
            if (fileName == null)
                return false;

            var languageOfCurrentFile = ParseFromFileName(fileName);
            return SupportedLanguages.Contains(languageOfCurrentFile);
        }
        
        /// <summary>
        /// Returns the Rosie language version string of the argument Codiga language.
        /// </summary>
        /// <param name="language">language the Codiga language to map to Rosie language</param>
        public static string GetRosieLanguage(LanguageEnumeration language)
        {
            return language switch
            {
                LanguageEnumeration.Python => "python",
                LanguageEnumeration.Javascript => "javascript",
                LanguageEnumeration.Typescript => "typescript",
                _ => "unknown"
            };
        }
    }
}
