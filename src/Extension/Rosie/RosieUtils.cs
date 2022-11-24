using static Extension.SnippetFormats.LanguageUtils;

namespace Extension.Rosie
{
    /// <summary>
    /// Utility for the Rosie integration.
    /// </summary>
    public static class RosieUtils
    {
        /// <summary>
        /// Returns the Rosie language version string of the argument Codiga language.
        /// </summary>
        /// <param name="language">language the Codiga language to map to Rosie language</param>
        public static string GetRosieLanguage(LanguageEnumeration language)
        {
            return language switch
            {
                LanguageEnumeration.Python => "python",
                _ => "unknown"
            };
        }
    }
}
