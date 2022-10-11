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
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Utilities;
using System.Web.UI.Design;

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
				using var xw = XmlWriter.Create(sw, new XmlWriterSettings { Encoding = Encoding.UTF8 });
				serializer.Serialize(xw, s);
				var xmlDoc = new DOMDocument();
				xmlDoc.loadXML(sw.ToString());
				var snippetNode = xmlDoc.documentElement.childNodes.nextNode();

				var snippetMoniker = Microsoft.VisualStudio.Imaging.KnownMonikers.Snippet;
				var imageElement = new ImageElement(new ImageId(snippetMoniker.Guid, snippetMoniker.Id));

				var item = new CompletionItem(s.CodeSnippet.Header.Shortcut, source, imageElement, ImmutableArray<CompletionFilter>.Empty, s.CodeSnippet.Header.Title);
				// store the XMLNode, description and id in the property bag so the ExpansionClient can access that later
				item.Properties.AddProperty(nameof(s.CodeSnippet.Snippet.Code), snippetNode);
				item.Properties.AddProperty(nameof(s.CodeSnippet.Header.Description), s.CodeSnippet.Header.Description);
				
				// enable to send usage mutation
				item.Properties.AddProperty(nameof(s.CodeSnippet.Header.Id), s.CodeSnippet.Header.Id);

				// add first snippet field to handle selection later
				if(s.CodeSnippet.Snippet.Declarations.Any())
					item.Properties.AddProperty(nameof(s.CodeSnippet.Snippet.Declarations),s.CodeSnippet.Snippet.Declarations.First().ID);

				return item;

			}).ToImmutableArray();
		}

		public static VisualStudioSnippet FromCodigaSnippet(CodigaSnippet codigaSnippet)
		{
			var vsSnippet = new VisualStudioSnippet
			{
				// TODO metdadata
				CodeSnippet = new CodeSnippet
				{
					Format = "1.0.0",
					Header = new Header
					{
						Id = codigaSnippet.Id,
						Title = codigaSnippet.Name,
						Author = "",
						Description = codigaSnippet.Description,
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

			var stringBuilder = new StringBuilder(plainCode);

			ReplaceUserCaretPositions(stringBuilder);
			ReplaceUserVariables(stringBuilder, vsSnippet);
			ReplaceIndentation(stringBuilder);

			vsSnippet.CodeSnippet.Snippet.Code = new Code(codigaSnippet.Language, stringBuilder.ToString());

			return vsSnippet;
		}



		/// <summary>
		/// Replaces Codiga user variables [USER_INPUT:order:default] with VS literals
		/// </summary>
		/// <param name="stringBuilder"></param>
		/// <param name="vsSnippet"></param>
		internal static void ReplaceUserVariables(StringBuilder stringBuilder, VisualStudioSnippet vsSnippet)
		{
			var plainCode = stringBuilder.ToString();

			var userInputRegex = new Regex(@"&\[USER_INPUT\:\d+\:[a-zA-Z0-9_]*\]");
			var variablesMatches = userInputRegex.Matches(plainCode);

			var userVariables = variablesMatches.Cast<Match>().Select(m => {

				var parts = m.Value.Split(':');

				return new CodigaUserVariable
				{
					Order = int.Parse(parts[1]),
					Default = parts[2].Substring(0, parts[2].Length - 1),
					PlaceholderText = m.Value
				};
			}).OrderBy(v => v.Order);

			// add literals and replace placeholder text
			foreach (var variable in userVariables)
			{
				var literal = new Literal
				{
					ID = $"param{variable.Order}",
					Default = variable.Default
				};

				if (!vsSnippet.CodeSnippet.Snippet.Declarations.Any(l => l.ID == literal.ID))
					vsSnippet.CodeSnippet.Snippet.Declarations.Add(literal);

				stringBuilder.Replace(variable.PlaceholderText, $"${literal.ID}$");
			}
		}

		/// <summary>
		/// Replaces Codiga user variables [USER_INPUT:order] without default with VS $end$
		/// as Visual Studio does not support variables without default.
		/// </summary>
		/// <param name="stringBuilder"></param>
		internal static void ReplaceUserCaretPositions(StringBuilder stringBuilder)
		{
			// Visual Studio only supports one $end$ variable
			// which sets the selection at the end of the session
			// so $end$ does not work with multiple caret positions
			// whitespace default does not work for VS.

			//TODO better support variables without default: generate default default? $end$ destorys the order

			var plainCode = stringBuilder.ToString();
			var userCaretRegex = new Regex(@"&\[USER_INPUT\:\d+\]");
			var caretMatches = userCaretRegex.Matches(plainCode);

			foreach (Match match in caretMatches)
			{
				stringBuilder.Replace(match.Value, "$end$");
			}
		}

		internal static void ReplaceIndentation(StringBuilder stringBuilder)
		{
			stringBuilder.Replace("&[CODIGA_INDENT]", "\t");
		}

		/// <summary>
		/// Represents the Codiga user variables that allow user defined placeholders
		/// </summary>
		private class CodigaUserVariable
		{
			public string PlaceholderText { get; set; }
			public int Order { get; set; }
			public string Default { get; set; }
		}

	}

	public static class CodigaLanguages
	{
		public enum LanguageEnumeration
		{
			Unknown,
			Coldfusion,
			Docker,
			Objectivec,
			Terraform,
			Json,
			Yaml,
			Typescript,
			Swift,
			Solidity,
			Sql,
			Shell,
			Scala,
			Rust,
			Ruby,
			Php,
			Python,
			Perl,
			Kotlin,
			Javascript,
			Java,
			Html,
			Haskell,
			Go,
			Dart,
			Csharp,
			Css,
			Cpp,
			C,
			Apex
		}

		public static string Parse(IContentType contentType)
		{
			return contentType.TypeName switch
			{
				"CSharp" => "Csharp",
				"CSS" => "Css",
				"HTML" => "Html",
				"HTMLProjection" => "Html",
				"JSON" => "Json",
				_ => "unknown"
			};
		}
	}
}
