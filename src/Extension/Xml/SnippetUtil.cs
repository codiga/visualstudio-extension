using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.VisualStudio.Text.Differencing;
using Match = System.Text.RegularExpressions.Match;
using System.Text;

namespace Extension.Xml
{

	public static class SnippetUtil
	{
		public static VisualStudioSnippet FromCodigaSnippet(CodigaSnippet codigaSnippet)
		{
			var vsSnippet = new VisualStudioSnippet
			{
				CodeSnippet = new CodeSnippet
				{
					Format = "1.0.0",
					Header = new Header
					{
						Shortcut = codigaSnippet.Shortcut,
						SnippetTypes = new SnippetTypes { SnippetType = "Expansion" }
					},
					Snippet = new Snippet
					{
						Declarations = new List<Literal>()
					}
				}
			};

			// get literals / user variables
			var re = new Regex(@"&\[USER_INPUT\:\d+\:[a-zA-Z0-9]*\]");
			var matches = re.Matches(codigaSnippet.Code);

			var userVariables = matches.Cast<Match>().Select(m => {

				var parts = m.Value.Split(':');

				return new UserVariable
				{
					Order = int.Parse(parts[1]),
					Default = parts[2].Substring(0, parts[2].Length - 1),
					PlaceholderText = m.Value
				};
			}).OrderBy(v => v.Order);

			// add literals and replace placeholder text
			StringBuilder builder = new StringBuilder(codigaSnippet.Code);
			foreach (var variable in userVariables)
			{
				vsSnippet.CodeSnippet.Snippet.Declarations.Add(new Literal
				{
					ID = $"param{variable.Order}",
					Default = variable.Default
				});

				builder.Replace(variable.PlaceholderText, $"\"$param{variable.Order}\"");
			}

			// format code to match Visual Studio format
			builder.Insert(0, "<![CDATA[");
			builder.Append("]]>");

			vsSnippet.CodeSnippet.Snippet.Code = new Code
			{
				Language = "CSharp",
				Text = builder.ToString()
			};

			return vsSnippet;
		}

		private class UserVariable
		{
			public string PlaceholderText{ get; set; }
			public int Order{ get; set; }
			public string Default { get; set; }
		}
		
	}

	public class CodigaSnippet
	{
		public string Shortcut { get; set; }
		public string Code { get; set; }

		public CodigaSnippet(string shortcut, string code)
		{
			Shortcut = shortcut;
			Code = code;
		}
	}
}
