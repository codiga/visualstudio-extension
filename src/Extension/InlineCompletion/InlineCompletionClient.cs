using Extension.Caching;
using Extension.SnippetFormats;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Extension.InlineCompletion
{
	internal class InlineCompletionClient : IOleCommandTarget
	{
		private IOleCommandTarget _nextCommandHandler;
		private InlineCompletionView _completionView;
		private IWpfTextView _textView;
		private IVsTextView _vsTextView;
		private ListNavigator<string> _snippetNavigator;

		public InlineCompletionClient(IWpfTextView textView, IVsTextView vsTextView, SnippetCache cache)
		{
			_textView = textView;
			_vsTextView = vsTextView;
			vsTextView.AddCommandFilter(this, out _nextCommandHandler);

			_snippetNavigator = new ListNavigator<string>(new[]
			{
				"public void MyMethod1()\n{\nblabla\nblabla\n}",
				"public void MyMethod2()\n{\nyadada\nyada\n}",
				"public void MyMethod3()\n{\ntext\n}"
			});
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}

		/// <summary>
		/// This handles incoming commands during the inline completion session.
		/// Commands are NextSnippet(tbd), PreviousSnippet(tbd), Commit [Tab], Cancel [ESC]
		/// </summary>
		/// <param name="pguidCmdGroup"></param>
		/// <param name="nCmdID"></param>
		/// <param name="nCmdexecopt"></param>
		/// <param name="pvaIn"></param>
		/// <param name="pvaOut"></param>
		/// <returns></returns>
		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			var typedChar = char.MinValue;
			//make sure the input is a char before getting it
			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
			{
				typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
			}

			if(_completionView != null)
			{
				return HandleSessionCommand(nCmdID);
			}

			_vsTextView.GetBuffer(out var textLines);
			_vsTextView.GetCaretPos(out var startLine, out var endColumn);
			textLines.GetLineText(startLine, 0, startLine, endColumn, out var line);

			var shouldTriggerCompletion = char.IsWhiteSpace(typedChar) 
				&& EditorUtils.IsSemanticSearchComment(line)
				&& _completionView == null;

			if (!shouldTriggerCompletion)
			{
				var result = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				return result;
			}

			// start inline completion session
			// create adornment
			_completionView = new InlineCompletionView(_textView);
			var caretPos = _textView.Caret.Position.BufferPosition;
			var currentLine = _textView.TextViewLines.Single(l => caretPos.Position >= l.Start && caretPos.Position <= l.End);
			_completionView.CreateCompletionView(currentLine, _snippetNavigator.First());

			return VSConstants.S_OK;
		}

		private int CommitCurrentSnippet()
		{
			return VSConstants.S_OK;
		}

		private int HandleSessionCommand(uint nCmdID)
		{
			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
			{
				_completionView.RemoveVisuals();
				_completionView = null;
				CommitCurrentSnippet();
				return VSConstants.S_OK;
			}

			if(nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
			{
				_completionView.RemoveVisuals();
				_completionView = null;
				return VSConstants.S_OK;
			}

			if(nCmdID == (uint)VSConstants.VSStd2KCmdID.RIGHT)
			{
				// get next snippet
				_completionView.UpdateSnippetPreview(_snippetNavigator.Next());
				return VSConstants.S_OK;
			}

			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.LEFT)
			{
				// get previous snippet
				_completionView.UpdateSnippetPreview(_snippetNavigator.Previous());
				return VSConstants.S_OK;
			}

			return VSConstants.S_OK;
		}
	}
}
