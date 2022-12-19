using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Extension.Logging;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

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
		        return wpfTextView.TextBuffer.GetFileName();
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