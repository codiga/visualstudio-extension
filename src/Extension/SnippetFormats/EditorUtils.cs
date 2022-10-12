using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension.SnippetFormats
{
	public static class EditorUtils
	{
		/// <summary>
		/// Checks if the input text contains any readable characters.
		/// </summary>
		/// <param name="textBeforeCaret"></param>
		/// <returns></returns>
		public static bool IsStartOfLine(string textBeforeCaret)
		{
			return string.IsNullOrEmpty(textBeforeCaret.Trim());
		}

		/// <summary>
		/// Checks if the line starts with the language specific comment characters.
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		public static bool IsComment(string line)
		{
			// TODO add support for all languages
			line = line.Trim();
			return line.StartsWith("//");
		}

		/// <summary>
		/// A semantic search comment is comment line that constist of at least two keywords.
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		public static bool IsSemanticSearchComment(string line)
		{
			if (!IsComment(line))
				return false;
			
			var withoutCommentChar = line.Replace("//", "").Trim();
			var keywords = withoutCommentChar.Split(' ');

			return keywords.Length >= 2;
		}
	}
}
