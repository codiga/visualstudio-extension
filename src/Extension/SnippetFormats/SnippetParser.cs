using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Linq;
using Match = System.Text.RegularExpressions.Match;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text.Adornments;
using GraphQLClient;
using Microsoft.VisualStudio.Core.Imaging;

namespace Extension.SnippetFormats
{
	/// <summary>
	/// General util class for the different snippet formats
	/// </summary>
	public static class SnippetParser
	{
		/// <summary>
		/// Creates completion items out of the provided Visual Studio snippets by adding an image and adding the snippet to the property bag.
		/// </summary>
		/// <param name="vsSnippets"></param>
		/// <param name="source"></param>
		/// <returns></returns>
		public static ImmutableArray<CompletionItem> FromVisualStudioSnippets(IEnumerable<VisualStudioSnippet> vsSnippets, IAsyncCompletionSource source)
		{
			return vsSnippets.Select(s =>
			{
				var snippetMoniker = Microsoft.VisualStudio.Imaging.KnownMonikers.Snippet;
				var imageElement = new ImageElement(new ImageId(snippetMoniker.Guid, snippetMoniker.Id));

				var item = new CompletionItem(s.CodeSnippet.Header.Shortcut, source, imageElement, ImmutableArray<CompletionFilter>.Empty, s.CodeSnippet.Header.Title);

				// store the snippet, in the property bag so the ExpansionClient can access that later
				item.Properties.AddProperty(nameof(s.CodeSnippet.Snippet), s);
				return item;

			}).ToImmutableArray();
		}

		/// <summary>
		/// Creates Visual Studio snippets out of the provided Codiga snippet.
		/// Also takes care of replacing all Codiga-specific variables in the code.
		/// </summary>
		/// <param name="codigaSnippet"></param>
		/// <param name="settings"></param>
		/// <returns></returns>
		public static VisualStudioSnippet FromCodigaSnippet(CodigaSnippet codigaSnippet, IndentationSettings settings)
		{
			var vsSnippet = new VisualStudioSnippet
			{
				CodeSnippet = new CodeSnippet
				{
					Format = "1.0.0",
					Header = new Header
					{
						Id = codigaSnippet.Id,
						Title = codigaSnippet.Name,
						Author = codigaSnippet.Owner?.DisplayName ?? "",
						Description = codigaSnippet.Description,
						Shortcut = codigaSnippet.Shortcut,
						SnippetTypes = new SnippetTypes { SnippetType = "Expansion" },
						Keywords = new List<Keyword>(codigaSnippet.Keywords.Select(k => new Keyword { Text = k})),
						IsPublic = codigaSnippet.IsPublic ?? false,
						IsPrivate = !codigaSnippet.IsPublic ?? true,
						IsSubscribed = codigaSnippet.IsSubscribed ?? false,
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
			ReplaceIndentation(stringBuilder, settings);

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

		/// <summary>
		/// Replaces Codiga Indention &[CODIGA_INDENT] with indention from the editor settings.
		/// Currently only supports 3 levels of indention.
		/// </summary>
		/// <param name="stringBuilder"></param>
		/// <param name="settings"></param>
		internal static void ReplaceIndentation(StringBuilder stringBuilder, IndentationSettings settings)
		{
			// Visual Studio supports defining tab size and indent size seperatly
			// so one codiga indent can not always be replaced with one VS indent 1:1

			// TODO replace hard coded level with algorithm
			var vsIndentLevel1 = EditorUtils.GetIndent(1, settings);
			var vsIndentLevel2 = EditorUtils.GetIndent(2, settings);
			var vsIndentLevel3 = EditorUtils.GetIndent(3, settings);

			stringBuilder.Replace("&[CODIGA_INDENT]&[CODIGA_INDENT]&[CODIGA_INDENT]", vsIndentLevel3);
			stringBuilder.Replace("&[CODIGA_INDENT]&[CODIGA_INDENT]", vsIndentLevel2);
			stringBuilder.Replace("&[CODIGA_INDENT]", vsIndentLevel1);
		}

		/// <summary>
		/// Gets the snippet code string and replaces all VS literals with their default value
		/// so that the preview is valid code.
		/// </summary>
		/// <param name="vsSnippetCode"></param>
		/// <returns></returns>
		internal static string GetPreviewCode(VisualStudioSnippet vsSnippet)
		{
			var literals = vsSnippet.CodeSnippet.Snippet.Declarations;
			var sb = new StringBuilder(vsSnippet.CodeSnippet.Snippet.Code.RawCode);

			foreach (var literal in literals)
			{
				sb.Replace($"${literal.ID}$", literal.Default);
			}
			sb.Replace("$end$", "");

			return sb.ToString();
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

	
}
