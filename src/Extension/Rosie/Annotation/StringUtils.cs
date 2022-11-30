using System;

namespace Extension.Rosie.Annotation
{
    internal static class StringUtils
    {
        /// <summary>
        /// Returns whether the two strings are equal ignoring their cases.
        /// </summary>
        /// <param name="s1">The first string to compare.</param>
        /// <param name="s2">The second string to compare.</param>
        /// <returns>True if the strings are equal ignoring cases, false otherwise.</returns>
        internal static bool AreEqualIgnoreCase(string s1, string s2)
        {
            return s1.Equals(s2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
