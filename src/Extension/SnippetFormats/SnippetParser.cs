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
		private static readonly Regex UserInputWithDefaultRegex =
			new Regex(@"&\[USER_INPUT\:(?<order>\d+)(\:(?<default>[a-zA-Z0-9_]*))]");
		private static readonly Regex UserInputWithoutDefaultRegex = new Regex(@"&\[USER_INPUT\:\d+]");
		
		/// <summary>
		/// The delimiter must be single-character because in case of multi-character delimiters, the parameters are not resolved properly
		/// upon expansion. The inserted parameter will look like e.g. <c>$!param1$!</c> for the delimiter <c>$!</c>.
		/// <br/>
		/// The delimiter must be a symbol that is possibly not or not often used in code. The default delimiter is <c>$</c>,
		/// but since $ is used in many programming languages, non-snippet-parameter $ symbols in Codiga snippets would break the
		/// expansion of the snippet.
		/// <br/>
		/// Although it is possible to escape $ symbols and treat them as literals by specifying them as $$, it poses problems when
		/// we want to include multiple $ symbols in the code, so it is safer to use a different delimiter.
		/// See https://stackoverflow.com/questions/3215705/escaping-the-character-in-snippets
		/// </summary>
		private const string ParameterDelimiter = "ß";
		
		/// <summary>
		/// Wraps the argument parameter name with the current delimiter, e.g. <c>paramName</c> becomes <c>ßparamNameß</c>. 
		/// </summary>
		private static readonly Func<string, string> SnippetParameter = name => $"{ParameterDelimiter}{name}{ParameterDelimiter}";
		
		/// <summary>
		/// Although <c>$end$</c> is a reserved parameter name, the delimiters in it must still match the delimiter that is set for the entire snippet.
		/// </summary>
		private static readonly string End = SnippetParameter("end");

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
				//This makes it possible to have the Codiga icon displayed in front of each snippet name in the shortcut completion list,
				// and also that the CompletionItems can be identified as Codiga snippets.
				var imageElement = new ImageElement(new ImageId(CodigaImageMoniker.CodigaMoniker.Guid, CodigaImageMoniker.CodigaMoniker.Id));

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
		/// <param name="codigaSnippet">Holds information about the Codiga specific snippet that is beibg converted to a VisualStudioSnippet</param>
		/// <param name="settings">Indentation settings in the active document view</param>
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
						Description = codigaSnippet.Description,
						Shortcut = codigaSnippet.Shortcut,
						SnippetTypes = new SnippetTypes { SnippetType = "Expansion" },
						Keywords = new List<Keyword>(codigaSnippet.Keywords.Select(k => new Keyword { Text = k})),
						IsPublic = codigaSnippet.IsPublic ?? false,
						IsPrivate = !codigaSnippet.IsPublic ?? true,
						IsSubscribed = codigaSnippet.IsSubscribed ?? false,
					},
					Snippet = new Snippet()
				}
			};

			if(codigaSnippet.Owner != null)
			{
				vsSnippet.CodeSnippet.Header.Author = codigaSnippet.Owner.DisplayName;
			}

			var encoded = codigaSnippet.Code;
			var base64bytes = Convert.FromBase64String(encoded);
			var plainCode = Encoding.UTF8.GetString(base64bytes);

			var codeWithResolvedVariables = new StringBuilder(plainCode);

			ReplaceUserCaretPositions(codeWithResolvedVariables);
			ReplaceUserVariables(codeWithResolvedVariables, vsSnippet);
			ReplaceIndentation(codeWithResolvedVariables, settings);

			vsSnippet.CodeSnippet.Snippet.Code = new Code(codigaSnippet.Language, codeWithResolvedVariables.ToString(), ParameterDelimiter);

			return vsSnippet;
		}

		/// <summary>
		/// Replaces Codiga user variables <c>[USER_INPUT:order]</c> without default with the reserved <c>end</c> variable
		/// as Visual Studio does not support variables without default.
		/// <br/>
		/// The replacement <c>end</c> variable is wrapped with the current <see cref="ParameterDelimiter"/>.
		/// </summary>
		/// <param name="codeWithResolvedVariables">Stores the code in which the Codiga user variables are replaced with VS parameters.</param>
		internal static void ReplaceUserCaretPositions(StringBuilder codeWithResolvedVariables)
		{
			// Visual Studio only supports one $end$ variable
			// which sets the selection at the end of the session
			// so $end$ does not work with multiple caret positions
			// whitespace default does not work for VS.

			var plainCode = codeWithResolvedVariables.ToString();
			var caretMatches = UserInputWithoutDefaultRegex.Matches(plainCode);

			// if the snippet does not define the cursor position after the session we set it to the end
			if(caretMatches.Count == 0)
			{
				codeWithResolvedVariables.Append(End);
			}

			foreach (Match match in caretMatches)
			{
				codeWithResolvedVariables.Replace(match.Value, End);
			}
		}

		/// <summary>
		/// Replaces Codiga user variables <c>[USER_INPUT:order:default]</c> with Visual Studio snippet specific literals,
		/// like <c>ßparam1ß</c> based on the current <see cref="ParameterDelimiter"/>.
		/// <br/>
		/// The indexing in the param name is coming from the order part of the USER_INPUT variable. For example in case of
		/// <c>[USER_INPUT:2:default value]</c> the param name is <c>param2</c>.
		/// <br/>
		/// This method doesn't handle the case when the default value is missing because it is handled before calling this method,
		/// in <see cref="ReplaceUserCaretPositions"/>.
		/// </summary>
		/// <param name="codeWithResolvedVariables">Stores the code in which the Codiga user variables are replaced with VS parameters.</param>
		/// <param name="vsSnippet">The snippet based on which expansion will happen</param>
		internal static void ReplaceUserVariables(StringBuilder codeWithResolvedVariables, VisualStudioSnippet vsSnippet)
		{
			var plainCode = codeWithResolvedVariables.ToString();
			var variablesMatches = UserInputWithDefaultRegex.Matches(plainCode);

			var userVariables = variablesMatches.Cast<Match>().Select(m => 
				new CodigaUserVariable
				{
					Order = int.Parse(m.Groups["order"].Value),
					Default = m.Groups["default"].Value.Substring(0, m.Groups["default"].Value.Length),
					PlaceholderText = m.Value //The entire text of the user variable e.g. &[USER_INPUT:2:value]
				}).OrderBy(v => v.Order);

			// add literals and replace placeholder text
			foreach (var variable in userVariables)
			{
				var literal = new Literal
				{
					ID = $"param{variable.Order}",
					Default = variable.Default
				};

				//If the snippet parameter is not yet added to the snippet, add it
				if (!vsSnippet.CodeSnippet.Snippet.Declarations.Any(l => l.ID == literal.ID))
					vsSnippet.CodeSnippet.Snippet.Declarations.Add(literal);

				codeWithResolvedVariables.Replace(variable.PlaceholderText, SnippetParameter(literal.ID));
			}
		}

		/// <summary>
		/// Replaces Codiga Indentation <c>&[CODIGA_INDENT]</c> with indentation from the editor settings.
		/// Currently only supports 3 levels of indentation.
		/// </summary>
		/// <param name="codeWithResolvedVariables">Stores the code in which the Codiga user variables are replaced with VS parameters.</param>
		/// <param name="settings">Indentation settings in the active document view</param>
		internal static void ReplaceIndentation(StringBuilder codeWithResolvedVariables, IndentationSettings settings)
		{
			// Visual Studio supports defining tab size and indent size separately
			// so one codiga indent can not always be replaced with one VS indent 1:1

			var vsIndentLevel1 = EditorUtils.GetIndent(1, settings);
			var vsIndentLevel2 = EditorUtils.GetIndent(2, settings);
			var vsIndentLevel3 = EditorUtils.GetIndent(3, settings);

			codeWithResolvedVariables.Replace("&[CODIGA_INDENT]&[CODIGA_INDENT]&[CODIGA_INDENT]", vsIndentLevel3);
			codeWithResolvedVariables.Replace("&[CODIGA_INDENT]&[CODIGA_INDENT]", vsIndentLevel2);
			codeWithResolvedVariables.Replace("&[CODIGA_INDENT]", vsIndentLevel1);
		}

		/// <summary>
		/// Gets the snippet code string and replaces all VS literals with their default value
		/// so that the preview is valid code.
		/// </summary>
		/// <param name="vsSnippet">The snippet based on which expansion will happen</param>
		/// <returns>The preview code string</returns>
		internal static string GetPreviewCode(VisualStudioSnippet vsSnippet)
		{
			var literals = vsSnippet.CodeSnippet.Snippet.Declarations;
			var sb = new StringBuilder(vsSnippet.CodeSnippet.Snippet.Code.RawCode);

			foreach (var literal in literals)
			{
				sb.Replace(SnippetParameter(literal.ID), literal.Default);
			}
			sb.Replace(End, "");

			return sb.ToString();
		}

		/// <summary>
		/// Parses the incoming "using" statement into a assembly name to be used in the Visual Studio Snippet. 
		/// See https://learn.microsoft.com/en-us/visualstudio/ide/code-snippets-schema-reference?view=vs-2022#assembly-element
		/// </summary>
		/// <param name="codigaImport">Import statements</param>
		internal static IEnumerable<Reference> GetClrReference(IReadOnlyCollection<string> codigaImport)
		{
			foreach (var import in codigaImport)
			{
				var sb = new StringBuilder(import);
				sb.Replace("using ","");
				sb.Replace(";", ".dll");
				yield return new Reference(sb.ToString());
			}
		}

		/// <summary>
		/// Represents the Codiga user variables that allow user defined placeholders
		/// </summary>
		private class CodigaUserVariable
		{
			public string PlaceholderText { get; set; }
			public int Order { get; set; }
			public string Default { get; set; }

			public CodigaUserVariable()
			{
				PlaceholderText = string.Empty; 
				Order = 0; 
				Default = string.Empty;	
			}
		}
	}
}
