using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Extension.InlineCompletion;
using Extension.Logging;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Extension.AssistantCompletion
{
	/// <summary>
	/// The simplest implementation of IAsyncCompletionCommitManager that provides Commit Characters and uses default behavior otherwise
	/// </summary>
	internal class ShortcutCompletionCommitManager : IAsyncCompletionCommitManager
    {
		ImmutableArray<char> commitChars = new char[] { ' ', '\'', '"', ',', '.', ';', ':' }.ToImmutableArray();

        public IEnumerable<char> PotentialCommitCharacters => commitChars;

        public IVsEditorAdaptersFactoryService VsEditorAdapter { get; }
		public ExpansionClient ExpansionClient { get; }


		public ShortcutCompletionCommitManager(IVsEditorAdaptersFactoryService vsEditorAdapter, ExpansionClient expansionClient)
		{
			// Use property here as we cannot import MEF Service here.
			VsEditorAdapter = vsEditorAdapter;
			ExpansionClient = expansionClient;
		}

		public bool ShouldCommitCompletion(IAsyncCompletionSession session, SnapshotPoint location, char typedChar, CancellationToken token)
        {
			return true;
        }

		// This method runs synchronously
        public CommitResult TryCommit(IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
        {
	        /*
	         * This adds an extra step above retrieving the VisualStudioSnippet from the CompletionItem.
	         * Since this commit manager is supposed to commit Codiga snippets only, we identify the CompletionItem whether it is a Codiga one,
	         * by its icon id, and if, for some reason, it is not a Codiga snippet, we don't handle it here. Instead, we let it be handled by other commit managers.
	         *
	         * This way, if a Codiga snippet is about to be committed (identified by its icon id), but its underlying VisualStudioSnippet is still not
	         * available, we can report it as an actual issue.
	         *
	         * See https://github.com/microsoft/vs-editor-api/issues/9: 
	         * CommitResult.IsHandled "indicates whether the item was committed - if not, Editor will call TryCommit on another IAsyncCompletionCommitManager".
	         */
	        if (item.Icon.ImageId.Id != CodigaImageMoniker.CodigaMoniker.Id)
		        return CommitResult.Unhandled;
	        
	        // start a snippet session using in memory xml rather than .xml files
	        try
	        {
		        // use IVsEditorAdaptersFactoryService to get access to IVsTextview
		        var vsTextView = VsEditorAdapter.GetViewAdapter(session.TextView);
		        var wpfTextView = VsEditorAdapter.GetWpfTextView(vsTextView);

		        var success =
			        item.Properties.TryGetProperty<VisualStudioSnippet>(nameof(VisualStudioSnippet.CodeSnippet.Snippet),
				        out var snippet);

		        if (!success)
			        HandleSnippetNotFoundException(wpfTextView, item);

		        ExpansionClient.StartExpansion(wpfTextView, snippet, true);
	        }
	        catch (CouldNotFindSnippetException e)
	        {
		        ExtensionLogger.LogException(e, e.FileExtension, e.SnippetName);
	        }
			catch(Exception e)
			{
				ExtensionLogger.LogException(e);
			}

			// we handled the completion by starting an expansion session so no other handlers should participate
			return CommitResult.Handled;
        }

        private static void HandleSnippetNotFoundException(IWpfTextView? wpfTextView, CompletionItem item)
        {
	        if (wpfTextView == null)
		        throw new CouldNotFindSnippetException("Could not find VisualStudioSnippet in property bag", nameof(item),
			        "No IWpfTextView", item.DisplayText);

	        var fileName = ThreadHelper.JoinableTaskFactory.Run(async () =>
	        {
		        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		        var doc = EditorUtils.ToDocumentView(wpfTextView);
		        return DocumentHelper.GetFileName(doc, wpfTextView);
	        });
	        
	        var fileExtension = fileName != null
		        ? Path.GetExtension(fileName) ?? "No file extension"
		        : "No file name";
	        
	        throw new CouldNotFindSnippetException("Could not find VisualStudioSnippet in property bag", nameof(item),
		        fileExtension, item.DisplayText);
        }

        private sealed class CouldNotFindSnippetException : ArgumentException
        {
	        public string FileExtension { get; }
	        public string SnippetName { get; }

	        public CouldNotFindSnippetException(string message, string paramName, string fileExtension, string snippetName) : base(message, paramName)
	        {
		        FileExtension = fileExtension;
		        SnippetName = snippetName;
	        }
        }  
    }
}