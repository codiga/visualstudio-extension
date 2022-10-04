using Microsoft.VisualStudio;
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
using static Microsoft.VisualStudio.Shell.ThreadedWaitDialogHelper;

namespace Extension.AssistantCompletion
{
	[Export]
	internal class ExpansionClient: IOleCommandTarget, IVsExpansionClient
	{
		private IVsExpansionSession CurrentExpansionSession;

		public int StartExpansion(IVsTextView vsTextView, CompletionItem completionItem)
		{
			vsTextView.GetBuffer(out var textLines);
			IVsExpansion expansion = (IVsExpansion)textLines;

			var position = new TextSpan();
			vsTextView.GetCaretPos(out var startLine, out var endColumn);

			position.iStartIndex = endColumn;
			position.iEndIndex = endColumn;
			position.iStartLine = startLine;
			position.iEndLine = startLine;

			//expansion.InsertSpecificExpansion(
			//	pSnippet: completionItem.Properties.GetProperty,
			//	tsInsertPos: position,
			//	pExpansionClient: this,
			//	guidLang: Guid.Empty,
			//	pszRelativePath: string.Empty,
			//	out CurrentExpansionSession);

			//vsTextView.AddCommandFilter

			return VSConstants.S_OK;
		} 

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			throw new NotImplementedException();
		}

		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			throw new NotImplementedException();
		}

		public int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc)
		{
			throw new NotImplementedException();
		}

		public int FormatSpan(IVsTextLines pBuffer, TextSpan[] ts)
		{
			throw new NotImplementedException();
		}

		public int EndExpansion()
		{
			throw new NotImplementedException();
		}

		public int IsValidType(IVsTextLines pBuffer, TextSpan[] ts, string[] rgTypes, int iCountTypes, out int pfIsValidType)
		{
			throw new NotImplementedException();
		}

		public int IsValidKind(IVsTextLines pBuffer, TextSpan[] ts, string bstrKind, out int pfIsValidKind)
		{
			throw new NotImplementedException();
		}

		public int OnBeforeInsertion(IVsExpansionSession pSession)
		{
			throw new NotImplementedException();
		}

		public int OnAfterInsertion(IVsExpansionSession pSession)
		{
			throw new NotImplementedException();
		}

		public int PositionCaretForEditing(IVsTextLines pBuffer, TextSpan[] ts)
		{
			throw new NotImplementedException();
		}

		public int OnItemChosen(string pszTitle, string pszPath)
		{
			throw new NotImplementedException();
		}
	}
}
