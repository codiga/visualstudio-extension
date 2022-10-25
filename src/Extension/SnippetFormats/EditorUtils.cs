using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Extension.SnippetFormats.LanguageUtils;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Extension.SnippetFormats
{
	public static class EditorUtils
	{

		public const string LineEnding = "\r\n";

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
		public static bool IsComment(string line, LanguageEnumeration language)
		{
			line = line.Trim();
			var sign = GetCommentSign(language);
			return line.StartsWith(sign);
		}

		/// <summary>
		/// A semantic search comment is comment line that constist of at least two keywords.
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		public static bool IsSemanticSearchComment(string line, LanguageEnumeration language)
		{
			if (!IsComment(line, language))
				return false;

			var sign = GetCommentSign(language);
			var withoutCommentChar = line.Replace(sign, "").Trim();
			var keywords = withoutCommentChar.Split(' ');

			return keywords.Length >= 2;
		}


		/// <summary>
		/// returns a new indented code block using the indentation of the current line and the settings.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="caret"></param>
		/// <param name="settings"></param>
		/// <returns></returns>
		public static string IndentCodeBlock(string code, ITextCaret caret, IndentationSettings settings)
		{
			var isVirtual = caret.InVirtualSpace;
			var snapshot = caret.Position.BufferPosition.Snapshot;
			int indentLevel;

			if (isVirtual)
			{
				var spaces = caret.Position.VirtualSpaces;
				indentLevel = GetVirtualIndentLevel(spaces, settings);
			}
			else
			{
				var currentLine = snapshot.GetLineFromPosition(caret.Position.BufferPosition.Position);
				indentLevel = GetIndentLevel(currentLine.GetText(), settings);
			}

			var indentedCode = IndentCodeBlock(code, indentLevel, settings, true);
			return indentedCode;
		}

		/// <summary>
		/// Returns a new indented code block based on the given IndentationSettings.
		/// The first line of the code block won't be indented as the editor should take care of that.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="settings"></param>
		/// <returns></returns>
		public static string IndentCodeBlock(string code, int indentLevel, IndentationSettings settings, bool indentFirstLine)
		{
			return IndentCodeBlock(code, indentLevel, settings.IndentSize, settings.TabSize, settings.UseSpace, indentFirstLine);
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
		public static string IndentCodeBlock(string code, int indentLevel, int indentSize, int tabSize, bool useSpace, bool indentFirstLine)
		{
			var lines = code.Split('\n');
			string finalIndent = GetIndent(indentLevel, indentSize, tabSize, useSpace);
						
			var i = indentFirstLine ? 0 : 1;
			for (; i < lines.Length; i++)
			{
				lines[i] = lines[i] + LineEnding;
				lines[i] = lines[i].Insert(0, finalIndent);
			}
			if(!indentFirstLine)
				lines[0] = lines[0] + LineEnding;

			var indentedCode = string.Concat(lines);
			return indentedCode;
		}

		public static int GetVirtualIndentLevel(int virtualSpaces, IndentationSettings settings)
		{
			var virtualLine = new string(' ', virtualSpaces);
			return GetIndentLevel(virtualLine, settings);
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
			var indentChars = string.Empty;

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
			
			var tabCount = charCounts.SingleOrDefault(c => c.Char == '\t')?.Count ?? 0;
			var spaceCount = charCounts.SingleOrDefault(c => c.Char == ' ')?.Count ?? 0;

			var indentLevel = (tabCount * tabSize + spaceCount) / indentSize;

			return indentLevel;
		}

		public static string GetIndent(int level, IndentationSettings settings)
		{
			return GetIndent(level, settings.IndentSize, settings.TabSize, settings.UseSpace);
		}

		/// <summary>
		/// Returns the ready-to-print indent for the given settings.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="indentSize"></param>
		/// <param name="tabSize"></param>
		/// <param name="useSpace"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Transforms the span into a new <see cref="TextSpan"/>.
		/// </summary>
		/// <param name="span"></param>
		/// <returns></returns>
		public static TextSpan GetLegacySpan(this SnapshotSpan span)
		{
			// sample model in current positions.
			// SnapshotSpan goes from 3 to 14.
			// expected legacy span would be: start: line 0, pos 3 end: line 2 pos 1
			/*
			 0: 0,1,2,{3,4,5,6[7]
			 1: 8,9,10,11,[12]
			 2: 13,14},[15]
			 */

			var textSpan = new TextSpan();
			
			var startLine = span.Snapshot.GetLineFromPosition(span.Span.Start);
			var endLine = span.Snapshot.GetLineFromPosition(span.Span.End);

			textSpan.iStartLine = startLine.LineNumber;
			textSpan.iEndLine = endLine.LineNumber;
			textSpan.iStartIndex = span.Start.Position - startLine.Start.Position;
			textSpan.iEndIndex = span.End.Position - endLine.Start.Position;

			return textSpan;
		}

		/// <summary>
		/// Returns a new TextSpan representing the caret position.
		/// </summary>
		/// <param name="caretPosition"></param>
		/// <returns></returns>
		public static TextSpan GetLegacyCaretPosition(this CaretPosition caretPosition)
		{
			var textSpan = new TextSpan();

			var caretLine = caretPosition.BufferPosition.GetContainingLine();

			// creating a new line with [Enter] will result in a virtual caret position. Only after entring text indent will be added.
			var caretIndex = (caretLine.Length + caretPosition.VirtualSpaces) - (caretLine.End.Position - caretPosition.BufferPosition.Position);

			textSpan.iStartLine = caretLine.LineNumber;
			textSpan.iEndLine = caretLine.LineNumber;
			textSpan.iStartIndex = caretIndex;
			textSpan.iEndIndex = caretIndex;

			return textSpan;
		}
	}
}
