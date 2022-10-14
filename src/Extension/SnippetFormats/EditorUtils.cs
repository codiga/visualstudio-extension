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

		/// <summary>
		/// Returns a new indented code block based on the given settings.
		/// The first line of the code block is expexted to be indented already.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="indentSize"></param>
		/// <param name="tabSize"></param>
		/// <param name="useSpace"></param>
		/// <returns></returns>
		public static string IndentCodeBlock(string code, int indentLevel, int indentSize, int tabSize, bool useSpace)
		{
			var lines = code.Split('\n');
			string spaceIndent = new string(' ', indentSize);
			string tabIndent = new string('\t', indentSize / tabSize);
			string finalIndent;

			if (useSpace)
			{
				finalIndent = string.Concat(Enumerable.Repeat(spaceIndent, indentLevel));
			}
			else
			{
				var tabs = string.Concat(Enumerable.Repeat(tabIndent, indentLevel));
				string spaces = "";

				if(tabSize > indentSize)
					spaces = string.Concat(Enumerable.Repeat(spaceIndent, indentLevel));

				finalIndent = tabs + spaces;
			}

			for (int i = 1; i < lines.Length; i++)
			{
				lines[i - 1] = lines[i - 1] + "\n";
				lines[i] = lines[i].Insert(0, finalIndent);
			}

			var indentedCode = string.Concat(lines);
			return indentedCode;
		}

		public static int GetIndentLevel(string line, int indentSize, int tabSize, bool useSpace)
		{
			//get non text chararcters
			var firstCharIndex = line.IndexOf(line.Trim().First());
			var indentChars = line.Substring(0, firstCharIndex);
			var charCounts = indentChars.GroupBy(c => c)
				.Select(c => new { Char = c.Key, Count = c.Count() });
			
			int tabCount = charCounts.SingleOrDefault(c => c.Char == '\t')?.Count ?? 0;
			int spaceCount = charCounts.SingleOrDefault(c => c.Char == ' ')?.Count ?? 0;

			int indentLevel = 0;
			if (useSpace)
			{
				indentLevel = spaceCount / indentSize;
			}
			else
			{
				indentLevel = (tabCount * tabSize + spaceCount) / indentSize;
			}

			return indentLevel;
		}
	}
}
