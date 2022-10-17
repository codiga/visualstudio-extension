using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

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
		/// Returns a new indented code block based on the given IndentationSettings.
		/// The first line of the code block won't be indented as the editor should take care of that.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="settings"></param>
		/// <returns></returns>
		public static string IndentCodeBlock(string code, int indentLevel, IndentationSettings settings)
		{
			return IndentCodeBlock(code, indentLevel, settings.IndentSize, settings.TabSize, settings.UseSpace);
		}

		/// <summary>
		/// Returns a new indented code block based on the given parameters.
		/// The first line of the code block won't be indented as the editor should take care of that.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="indentSize"></param>
		/// <param name="tabSize"></param>
		/// <param name="useSpace"></param>
		/// <returns></returns>
		public static string IndentCodeBlock(string code, int indentLevel, int indentSize, int tabSize, bool useSpace)
		{
			var lines = code.Split('\n');
			string finalIndent = GetIndent(indentLevel, indentSize, tabSize, useSpace);

			lines[0] = lines[0] + "\n";
			for (int i = 1; i < lines.Length; i++)
			{
				lines[i] = lines[i] + "\n";
				lines[i] = lines[i].Insert(0, finalIndent);
				
			}

			var indentedCode = string.Concat(lines);
			return indentedCode;
		}

		/// <summary>
		/// Calculates the indention level based on the given line and IndentationSettings.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="settings"></param>
		/// <returns></returns>
		public static int GetIndentLevel(string line, IndentationSettings settings)
		{
			return GetIndentLevel(line, settings.IndentSize, settings.TabSize, settings.UseSpace);
		}

		/// <summary>
		/// Calculates the indention level based on the given line and parameters.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="indentSize"></param>
		/// <param name="tabSize"></param>
		/// <param name="useSpace"></param>
		/// <returns></returns>
		public static int GetIndentLevel(string line, int indentSize, int tabSize, bool useSpace)
		{
			//get non text chararcters
			string indentChars = string.Empty;

			if(line.Any(c => c != ' ' && c != '\t'))
			{
				var firstCharIndex = line.IndexOf(line.Trim().First());
				indentChars = line.Substring(0, firstCharIndex);
			}
			else
			{
				indentChars = line;
			}
			
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

		public static string GetIndent(int level, IndentationSettings settings)
		{
			return GetIndent(level, settings.IndentSize, settings.TabSize, settings.UseSpace);
		}

		public static string GetIndent(int level, int indentSize, int tabSize, bool useSpace)
		{
			int totalIndentSize = level * indentSize;
			string finalIndent;

			if (useSpace)
			{
				finalIndent = new string(' ', totalIndentSize);
			}
			else
			{
				string tabIndent = new string('\t', totalIndentSize / tabSize);

				if (tabSize > indentSize)
					finalIndent = new string(' ', totalIndentSize);
				else
					finalIndent = tabIndent + new string(' ', totalIndentSize % tabSize);
			}

			return finalIndent;
		}
	}
}
