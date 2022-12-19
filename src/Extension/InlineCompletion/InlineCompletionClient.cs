using Extension.AssistantCompletion;
using Extension.Caching;
using Extension.Logging;
using Extension.SnippetFormats;
using GraphQLClient;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Extension.InlineCompletion
{
	/// <summary>
	/// This class is responsible for the inline completion session lifecycle.
	/// The main purpose is to provide the user with a good preview of potential snippets returned by the semantic search.
	/// The snippet insertion process is passed to the <see cref="ExpansionClient"/>
	/// </summary>
	internal class InlineCompletionClient : IOleCommandTarget, IDisposable
	{
		private IOleCommandTarget _nextCommandHandler;
		private IWpfTextView _wpfTextView;
		private ICodigaClientProvider _clientProvider;

		private InlineCompletionView? _completionView;
		private ListNavigator<VisualStudioSnippet>? _snippetNavigator; 

		private ExpansionClient? _expansionClient;

		/// <summary>
		/// Initialize the client and start listening for commands
		/// </summary>
		/// <param name="wpfTextView"></param>
		/// <param name="expansionClient"></param>
		public void Initialize(IWpfTextView wpfTextView, ExpansionClient expansionClient)
		{
			_clientProvider = new DefaultCodigaClientProvider();
			_wpfTextView = wpfTextView;
			var vsTextView = wpfTextView.ToIVsTextView();

			if (vsTextView == null)
				return;

			_expansionClient = expansionClient;
			vsTextView.AddCommandFilter(this, out _nextCommandHandler);
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			int res = 0;
			try
			{
				ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				});

				ThreadHelper.ThrowIfNotOnUIThread();

				res = _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
			}
			catch (Exception e)
			{
				ExtensionLogger.LogException(e);
			}
			return res;
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
			try
			{
				var codigaSettings = EditorSettingsProvider.GetCurrentCodigaSettings();

				if (!codigaSettings.UseInlineCompletion)
				{
					return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				}

				var typedChar = char.MinValue;
				//make sure the input is a char before getting it
				if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
				{
					typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
				}

				if (_completionView != null)
				{
					return HandleSessionCommand(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				}

				var caretPos = _wpfTextView.Caret.Position.BufferPosition.Position;
				var triggeringLine = _wpfTextView.TextBuffer.CurrentSnapshot.GetLineFromPosition(caretPos);
				var triggeringLineText = triggeringLine.GetText();
				var lineTrackingSpan = _wpfTextView.TextBuffer.CurrentSnapshot.CreateTrackingSpan(triggeringLine.Extent.Span, SpanTrackingMode.EdgePositive);
				var language = LanguageUtils.Parse(Path.GetExtension(_wpfTextView.ToDocumentView().FilePath));


				var shouldTriggerCompletion = char.IsWhiteSpace(typedChar)
					&& EditorUtils.IsSemanticSearchComment(triggeringLineText, language)
					&& _completionView == null;

				if (!shouldTriggerCompletion)
				{
					return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				}

				// start inline completion session

				// query snippets based on keywords
				var sign = LanguageUtils.GetCommentSign(language);
				var term = triggeringLineText.Replace(sign, "").Trim();

				var languages = new ReadOnlyCollection<string>(new[] { language.GetName() });

				if(!_clientProvider.TryGetClient(out var client))
					return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

				ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
				{
					try
					{
						await client.GetRecipesForClientSemanticAsync(term, languages, false, 10, 0)
						.ContinueWith(OnQueryFinished, TaskScheduler.Default);
					}
					catch (CodigaAPIException e)
					{
						ExtensionLogger.LogException(e);
					}
				});

				_completionView = new InlineCompletionView(_wpfTextView, lineTrackingSpan);

				// start drawing the adornments for the instructions
				_completionView.StartDrawingInstructions();

				return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}

			catch (Exception e)
			{
				ExtensionLogger.LogException(e);
				Dispose();
				return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}
		}

		/// <summary>
		/// Calls the ExpansionClient to insert the selected snippet.
		/// </summary>
		/// <returns></returns>
		private int CommitCurrentSnippet()
		{
			try
			{
				_expansionClient?.StartExpansion(_wpfTextView, _snippetNavigator.CurrentItem, true);
			}
			catch(Exception e)
			{
				ExtensionLogger.LogException(e);
				return VSConstants.S_FALSE;
			}
			
			return VSConstants.S_OK;
		}

		/// <summary>
		/// Callback for when the API query returns with its results.
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		private async Task OnQueryFinished(Task<System.Collections.Generic.IReadOnlyCollection<CodigaSnippet>> result)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			var setting = EditorSettingsProvider.GetCurrentIndentationSettings();
			var snippets = result.Result.Select(s => SnippetParser.FromCodigaSnippet(s, setting));

			if (_completionView == null)
				return;

			if (snippets.Any())
			{
				_snippetNavigator = new ListNavigator<VisualStudioSnippet>(snippets.ToList());
				var previewCode = SnippetParser.GetPreviewCode(_snippetNavigator.CurrentItem);
				var currentIndex = _snippetNavigator.IndexOf(_snippetNavigator.CurrentItem) + 1;
				_completionView.UpdateView(previewCode, currentIndex, _snippetNavigator.Count);
			}
			else
			{
				_completionView.ShowPreview = false;
				_completionView.UpdateView(null, 0, 0);
			}
		}

		/// <summary>
		/// Handles all the commands during an open inline session.
		/// </summary>
		/// <param name="nCmdID"></param>
		/// <returns></returns>
		private int HandleSessionCommand(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
			{
				_completionView?.RemoveInstructions();
				_completionView = null;
				CommitCurrentSnippet();
				return VSConstants.S_OK;
			}

			else if(nCmdID == (uint)VSConstants.VSStd2KCmdID.RIGHT)
			{
				if(_snippetNavigator == null || _snippetNavigator.Count == 0)
					return VSConstants.S_OK;

				// get next snippet
				var next = _snippetNavigator.Next();
				var i = _snippetNavigator.IndexOf(next);
				var c = _snippetNavigator.Count;

				var previewCode = SnippetParser.GetPreviewCode(next);
				_completionView?.UpdateView(previewCode, i + 1, c);

				return VSConstants.S_OK;
			}

			else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.LEFT)
			{
				if (_snippetNavigator == null || _snippetNavigator.Count == 0)
					return VSConstants.S_OK;

				// get previous snippet
				var previous = _snippetNavigator.Previous();
				var i = _snippetNavigator.IndexOf(previous);
				var c = _snippetNavigator.Count;

				var previewCode = SnippetParser.GetPreviewCode(previous); 
				_completionView?.UpdateView(previewCode, i + 1, c);

				return VSConstants.S_OK;
			}
			else if((pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR) || 
					nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
			{
				_completionView?.RemoveInstructions();
				_completionView = null;
				_snippetNavigator = null;
			}

			return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
		}

		public void Dispose()
		{
			_wpfTextView.ToIVsTextView()?.RemoveCommandFilter(this);
		}
	}
}
