﻿using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using MSXML;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extension.SnippetFormats;
using static Microsoft.VisualStudio.Shell.ThreadedWaitDialogHelper;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace Extension.AssistantCompletion
{
	/// <summary>
	/// This class is responsible for the ExpansionSession lifecycle.
	/// It initializes a new ExpansionSession and deals with user input during that session.
	/// A session ends as soon as the user commits the snippet insertion.
	/// </summary>
	[Export]
	internal class ExpansionClient: IOleCommandTarget, IVsExpansionClient
	{
		private IVsExpansionSession m_currentExpansionSession;
		private IOleCommandTarget m_nextCommandHandler;

		/// <summary>
		/// Starts a new snippet insertion session at the current caret position.
		/// </summary>
		/// <param name="vsTextView"></param>
		/// <param name="completionItem"></param>
		/// <returns></returns>
		public int StartExpansion(IVsTextView vsTextView, CompletionItem completionItem)
		{
			vsTextView.AddCommandFilter(this, out m_nextCommandHandler);

			vsTextView.GetBuffer(out var textLines);
			var expansion = (IVsExpansion)textLines;

			var position = new TextSpan();
			vsTextView.GetCaretPos(out var startLine, out var endColumn);

			position.iStartIndex = endColumn;
			position.iEndIndex = endColumn;
			position.iStartLine = startLine;
			position.iEndLine = startLine;

			var xmlSnippet = completionItem.Properties.GetProperty<IXMLDOMNode>(nameof(VisualStudioSnippet.CodeSnippet.Snippet.Code));
			var xml = xmlSnippet.xml;

			var snippet = SnippetInsertion.GetSnippet();

			expansion.InsertSpecificExpansion(
				pSnippet: xmlSnippet,
				tsInsertPos: position,
				pExpansionClient: this,
				guidLang: Guid.Empty,
				pszRelativePath: string.Empty,
				out m_currentExpansionSession);

			return VSConstants.S_OK;
		} 

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			return m_nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
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
			if (m_currentExpansionSession == null)
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
					m_currentExpansionSession.GoToPreviousExpansionField();
					return VSConstants.S_OK;
				}
				else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
				{

					m_currentExpansionSession.GoToNextExpansionField(0); //false to support cycling through all the fields
					return VSConstants.S_OK;
				}
				else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN || nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
				{
					if (m_currentExpansionSession.EndCurrentExpansion(0) == VSConstants.S_OK)
					{
						m_currentExpansionSession = null;
						return VSConstants.S_OK;
					}
				}
			}

			return VSConstants.S_OK;
		}

		public int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc)
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
			return VSConstants.S_OK;
		}

		public int IsValidType(IVsTextLines pBuffer, TextSpan[] ts, string[] rgTypes, int iCountTypes, out int pfIsValidType)
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
	}
}
