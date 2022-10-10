using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension.SnippetFormats
{
	public static class EditorUtils
	{
		public static bool IsStartOfLine(string textBeforeCaret)
		{
			var textWithoutIndent = textBeforeCaret.Replace("\t", "").Replace(" ", "");
			return string.IsNullOrEmpty(textWithoutIndent);
		}

		public static bool IsComment(string line)
		{
			return false;
		}
	}
}
