using Community.VisualStudio.Toolkit;
using EnvDTE;
using Extension.Caching;
using Extension.SnippetFormats;
using GraphQLClient;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Settings.Internal;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using MSXML;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Shapes;
using System.Xml;

namespace Extension.AssistantCompletion
{
	/// <summary>
	/// This class is responsible for the ExpansionSession lifecycle.
	/// It initializes a new ExpansionSession and deals with user input during that session.
	/// A session ends as soon as the user commits the snippet insertion.
	/// </summary>
	[Export]
	internal class ExpansionClient : IOleCommandTarget, IVsExpansionClient
	{
		private IVsExpansionSession _currentExpansionSession;
		private IOleCommandTarget _nextCommandHandler;
		private IVsTextView _currentTextView;

		private string? _firstUserVariable;
		private TextSpan _endSpan;

		private CodigaClient _client;

		/// <summary>
		/// Starts a new snippet insertion session at the current caret position.
		/// </summary>
		/// <param name="vsTextView"></param>
		/// <param name="completionItem"></param>
		/// <returns></returns>
		public int StartExpansion(IVsTextView vsTextView, VisualStudioSnippet snippet, CodigaClientProvider clientProvider)
		{
			_currentTextView = vsTextView;
			_endSpan = new TextSpan();
			_client = clientProvider.GetClient();
			_firstUserVariable = snippet.CodeSnippet.Snippet.Declarations.FirstOrDefault()?.ID;

			// start listening for incoming commands/keys
			vsTextView.AddCommandFilter(this, out _nextCommandHandler);

			vsTextView.GetBuffer(out var textLines);
			var expansion = (IVsExpansion)textLines;
			vsTextView.GetCaretPos(out var startLine, out var endColumn);
			
			// replace the typed search text
			textLines.GetLineText(startLine, 0, startLine, endColumn, out var currentLine);

			int startIndex;
			if (currentLine.Any(c => c != ' ' && c != '\t'))
			{
				startIndex = currentLine.IndexOf(currentLine.Trim().First());
			}
			else
			{
				startIndex = endColumn;
			}
			var position = new TextSpan
			{
				iStartLine = startLine,
				iStartIndex = startIndex,
				iEndLine = startLine,
				iEndIndex = endColumn,
			};

			var formattedSnippet = FormatSnippet(snippet.CodeSnippet.Snippet.Code.CodeString, currentLine);
			snippet.CodeSnippet.Snippet.Code.CodeString = formattedSnippet;

			textLines.GetLanguageServiceID(out var languageServiceId);

			// create IXMLDOMNode from snippet
			IXMLDOMNode snippetXml;
			var serializer = new System.Xml.Serialization.XmlSerializer(typeof(VisualStudioSnippet));
			using (var sw = new StringWriter())
			{
				using var xw = XmlWriter.Create(sw, new XmlWriterSettings { Encoding = Encoding.UTF8 });
				serializer.Serialize(xw, snippet);
				var xmlDoc = new DOMDocument();
				xmlDoc.loadXML(sw.ToString());
				snippetXml = xmlDoc.documentElement.childNodes.nextNode();
			}

			expansion.InsertSpecificExpansion(
				pSnippet: snippetXml,
				tsInsertPos: position,
				pExpansionClient: this,
				guidLang: languageServiceId,
				pszRelativePath: string.Empty,
				out _currentExpansionSession);
			
			ReportUsage(snippet.CodeSnippet.Header.Id);

			return VSConstants.S_OK;
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}

		/// <summary>
		/// Handle incoming user keys/commands to maily support jumping between user variables during a snippet session.
		/// </summary>
		/// <param name="pguidCmdGroup"></param>
		/// <param name="nCmdID"></param>
		/// <param name="nCmdexecopt"></param>
		/// <param name="pvaIn"></param>
		/// <param name="pvaOut"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			if (_currentExpansionSession == null)
				return VSConstants.S_OK;

			//make a copy of this so we can look at it after forwarding some commands
			var commandID = nCmdID;
			var typedChar = char.MinValue;
			//make sure the input is a char before getting it
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
			{
				typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
			}

			//check for a commit character
			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
			    || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB
			    || (char.IsWhiteSpace(typedChar)))
			{
				if (nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKTAB)
				{
					_currentExpansionSession.GoToPreviousExpansionField();
					return VSConstants.S_OK;
				}
				else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
				{

					_currentExpansionSession
						.GoToNextExpansionField(0); //false to support cycling through all the fields
					return VSConstants.S_OK;
				}
				else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN ||
				         nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
				{
					if (_currentExpansionSession.EndCurrentExpansion(0) == VSConstants.S_OK)
					{
						_currentExpansionSession = null;
						return VSConstants.S_OK;
					}
				}
			}

			//pass along the command so the char is added to the buffer
			var result = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

			return result;
		}

		public int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName,
			out IVsExpansionFunction pFunc)
		{
			pFunc = null;
			return VSConstants.S_OK;
		}

		public int FormatSpan(IVsTextLines pBuffer, TextSpan[] ts)
		{
			// NOTE: How to get the LanguageService here to call Source.Reformat?
			return VSConstants.S_OK;
		}

		public int EndExpansion()
		{
			// stop listening to input when the expansion is done
			_currentTextView.RemoveCommandFilter(this);
			return VSConstants.S_OK;
		}

		public int IsValidType(IVsTextLines pBuffer, TextSpan[] ts, string[] rgTypes, int iCountTypes,
			out int pfIsValidType)
		{

			pfIsValidType = 1;
			return VSConstants.S_OK;
		}

		public int IsValidKind(IVsTextLines pBuffer, TextSpan[] ts, string bstrKind, out int pfIsValidKind)
		{
			pfIsValidKind = 1;
			return VSConstants.S_OK;
		}

		public int OnBeforeInsertion(IVsExpansionSession pSession)
		{
			return VSConstants.S_OK;
		}

		public int OnAfterInsertion(IVsExpansionSession pSession)
		{
			return VSConstants.S_OK;
		}

		public int PositionCaretForEditing(IVsTextLines pBuffer, TextSpan[] ts)
		{
			return VSConstants.S_OK;
		}

		public int OnItemChosen(string pszTitle, string pszPath)
		{
			return VSConstants.S_OK;
		}

		/// <summary>
		/// Report snippet usage to the Codiga API to update snippet statistics
		/// </summary>
		/// <param name="id"></param>
		private void ReportUsage(long id)
		{
			_client.RecordRecipeUseAsync(id);
		}

		/// <summary>
		/// Formats snippet code based on the indention of the passed base line
		/// </summary>
		/// <param name="snippetCode"></param>
		/// <param name="baseLine"></param>
		/// <returns></returns>
		private string FormatSnippet(string snippetCode, string baseLine)
		{
			var settings = EditorSettingsProvider.GetCurrentIndentationSettings();
			var indentLevel = EditorUtils.GetIndentLevel(baseLine, settings);
			return EditorUtils.IndentCodeBlock(snippetCode, indentLevel, settings);
		}
	}
}
