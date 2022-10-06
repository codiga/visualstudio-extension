using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.VisualStudio.Text.Differencing;
using Match = System.Text.RegularExpressions.Match;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text.Adornments;
using MSXML;
using Microsoft.VisualStudio.Settings.Internal;
using System.IO;
using System.Xml;
using GraphQLClient;

namespace Extension.SnippetFormats
{

	public static class SnippetParser
	{
		public static ImmutableArray<CompletionItem> FromVisualStudioSnippets(IEnumerable<VisualStudioSnippet> vsSnippets, IAsyncCompletionSource source)
		{
			var serializer = new System.Xml.Serialization.XmlSerializer(typeof(VisualStudioSnippet));
			
			return vsSnippets.Select(s =>
			{
				// create IXMLDOMNode from snippet
				using var sw = new StringWriter();
				using var xw = XmlWriter.Create(sw, new XmlWriterSettings{Encoding = Encoding.UTF8});
				serializer.Serialize(xw, s);
				var xmlDoc = new DOMDocument();
				xmlDoc.loadXML(sw.ToString());
				var snippetNode = xmlDoc.documentElement.childNodes.nextNode();

				var item = new CompletionItem(s.CodeSnippet.Header.Shortcut, source);
				// store the XMLNode in the property bag so the ExpansionClient can access that later
				item.Properties.AddProperty(nameof(s.CodeSnippet.Snippet.Code), snippetNode);

				return item;

			}).ToImmutableArray();
		}

		public static VisualStudioSnippet FromCodigaSnippet(CodigaSnippet codigaSnippet)
		{
			var vsSnippet = new VisualStudioSnippet
			{
				CodeSnippet = new CodeSnippet
				{
					Format = "1.0.0",
					Header = new Header
					{
						Title = "tbd",
						Author = "tbd",
						Description = "tdb",
						Shortcut = codigaSnippet.Shortcut,
						SnippetTypes = new SnippetTypes { SnippetType = "Expansion" }
					},
					Snippet = new Snippet
					{
						Declarations = new List<Literal>()
					}
				}
			};

			var encoded = codigaSnippet.Code;
			var base64bytes = Convert.FromBase64String(encoded);
			var plainCode = Encoding.UTF8.GetString(base64bytes);

			// get literals / user variables
			// TODO fix does not work when no default value is provided
			var re = new Regex(@"&\[USER_INPUT\:\d+\:[a-zA-Z0-9]*\]");
			var matches = re.Matches(plainCode);

			var userVariables = matches.Cast<Match>().Select(m => {

				var parts = m.Value.Split(':');

				return new CodigaUserVariable
				{
					Order = int.Parse(parts[1]),
					Default = parts[2].Substring(0, parts[2].Length - 1),
					PlaceholderText = m.Value
				};
			}).OrderBy(v => v.Order);

			// add literals and replace placeholder text
			StringBuilder builder = new StringBuilder(plainCode);
			foreach (var variable in userVariables)
			{
				vsSnippet.CodeSnippet.Snippet.Declarations.Add(new Literal
				{
					ID = $"param{variable.Order}",
					Default = variable.Default
				});

				builder.Replace(variable.PlaceholderText, $"$param{variable.Order}$");
			}

			vsSnippet.CodeSnippet.Snippet.Code = new Code(codigaSnippet.Language, builder.ToString());

			return vsSnippet;
		}

		/// <summary>
		/// Represents the Codiga user variables that allow user defined placeholders
		/// </summary>
		private class CodigaUserVariable
		{
			public string PlaceholderText{ get; set; }
			public int Order{ get; set; }
			public string Default { get; set; }
		}
		
	}
}
