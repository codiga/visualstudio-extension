using Community.VisualStudio.Toolkit;
using EnvDTE;
using Extension.Caching;
using Extension.Logging;
using Extension.SnippetFormats;
using GraphQLClient;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Settings.Internal;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using MSXML;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.TextFormatting;
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
	internal class ExpansionClient : IOleCommandTarget, IVsExpansionClient, IDisposable
	{
		private IVsExpansionSession _currentExpansionSession;
		private IOleCommandTarget _nextCommandHandler;
		private IVsTextView _currentTextView;


		/// <summary>
		/// Starts a new snippet insertion based on the caret of the given TextView.
		/// </summary>
		/// <param name="textView"></param>
		/// <param name="snippet"></param>
		/// <returns></returns>
		public int StartExpansion(IWpfTextView textView, VisualStudioSnippet snippet, bool replaceLine)
		{
			var caret = textView.Caret;
			var code = snippet?.CodeSnippet?.Snippet?.Code?.RawCode;

			if (code == null)
				return VSConstants.S_FALSE;

			// indent code based on caret
			var settings = EditorSettingsProvider.GetCurrentIndentationSettings();
			var indentedCode = EditorUtils.IndentCodeBlock(code, caret, settings);
			snippet.CodeSnippet.Snippet.Code.CodeString = indentedCode;
			var currentLine = textView.TextSnapshot.GetLineFromPosition(caret.Position.BufferPosition.Position);

			// determine span for insertion, insert at caret or replace whole line
			// as the expansion client is a legacy API we have to transform the spans to TextSpan.
			TextSpan insertionPosition;
			if (replaceLine || (currentLine.GetText().All(c => c == ' ' || c == '\t')))
			{
						
				insertionPosition = currentLine.Extent.GetLegacySpan();
			}
			else
			{
				insertionPosition = caret.Position.GetLegacyCaretPosition();
			}

			StartExpansionInternal(textView.ToIVsTextView(), snippet, insertionPosition);

			return VSConstants.S_OK;
		}

		private int StartExpansionInternal(IVsTextView vsTextView, VisualStudioSnippet formattedSnippet, TextSpan position)
		{
			_currentTextView = vsTextView;

			// start listening for incoming commands/keys
			vsTextView.AddCommandFilter(this, out _nextCommandHandler);

			// create IXMLDOMNode from snippet
			IXMLDOMNode snippetXml;
			var serializer = new System.Xml.Serialization.XmlSerializer(typeof(VisualStudioSnippet));
			using (var sw = new StringWriter())
			{
				using var xw = XmlWriter.Create(sw, new XmlWriterSettings { Encoding = Encoding.UTF8 });
				serializer.Serialize(xw, formattedSnippet);
				var xmlDoc = new DOMDocument();
				xmlDoc.loadXML(sw.ToString());
				snippetXml = xmlDoc.documentElement.childNodes.nextNode();
			}

			vsTextView.GetBuffer(out var textLines);
			textLines.GetLanguageServiceID(out var languageServiceId);
			var expansion = (IVsExpansion)textLines;

			expansion.InsertSpecificExpansion(
				pSnippet: snippetXml,
				tsInsertPos: position,
				pExpansionClient: this,
				guidLang: languageServiceId,
				pszRelativePath: string.Empty,
				out _currentExpansionSession);

			ReportUsage(formattedSnippet.CodeSnippet.Header.Id);

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
			try
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

			catch( Exception e)
			{
				ExtensionLogger.LogException(e);
				Dispose();
				return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}
		}

		public int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName,
			out IVsExpansionFunction pFunc)
		{
			pFunc = null;
			return VSConstants.S_OK;
		}

		public int FormatSpan(IVsTextLines pBuffer, TextSpan[] ts)
		{
			return VSConstants.S_OK;
		}

		public int EndExpansion()
		{
			_currentTextView?.RemoveCommandFilter(this);
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
			var clientProvider = new DefaultCodigaClientProvider();

			if(!clientProvider.TryGetClient(out var client))
				return;
			
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				try
				{
					await client.RecordRecipeUseAsync(id);
				}
				catch (CodigaAPIException e)
				{
					ExtensionLogger.LogException(e);
					return;
				}
			});
		}

		public void Dispose()
		{
			_currentTextView?.RemoveCommandFilter(this);
			_currentExpansionSession?.EndCurrentExpansion(0);
			_currentExpansionSession = null;
		}
	}
}
