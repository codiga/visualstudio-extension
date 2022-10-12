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
			return string.IsNullOrEmpty(textBeforeCaret.Trim());
		}

		public static bool IsComment(string line)
		{
			// TODO add support for all languages
			line = line.Trim();
			return line.StartsWith("//");
		}
	}
}
