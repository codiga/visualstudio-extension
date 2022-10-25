using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Extension.Logging;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;

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

        public CommitResult TryCommit(IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
        {
			// This method runs synchronously

			// use IVsEditorAdaptersFactoryService to get access to IVsTextview
			var vsTextView = VsEditorAdapter.GetViewAdapter(session.TextView);
			var wpfTextView = VsEditorAdapter.GetWpfTextView(vsTextView);

			item.Properties.TryGetProperty<VisualStudioSnippet>(nameof(VisualStudioSnippet.CodeSnippet.Snippet), out var snippet);

			// get current editor settings to be able to format the code correctly

			// start a snippet session using in memory xml rather than .xml files
			try
			{
				ExpansionClient.StartExpansion(wpfTextView, snippet, true);
			}
			catch(Exception e)
			{
				ExtensionLogger.LogException(e);
			}

			// we handled the completion by starting an expansion session so no other handlers should participate
			return CommitResult.Handled;
        }
    }
}