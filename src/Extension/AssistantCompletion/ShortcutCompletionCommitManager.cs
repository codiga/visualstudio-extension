﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
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
			// This method runs synchronously, potentially before CompletionItem has been computed.
			// The purpose of this method is to filter out characters not applicable at given location.

			// This method is called only when typedChar is among the PotentialCommitCharacters
			// in this simple example, all PotentialCommitCharacters do commit, so we always return true
			//session.TextView

			return true;
        }

        public CommitResult TryCommit(IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
        {
			// Objects of interest here are session.TextView and session.TextView.Caret.
			// This method runs synchronously

			// insert specific expansion
			// start expansion session
			// pass key 

			// use IVsEditorAdaptersFactoryService to get access to IVsTextview
			var vsTextView = VsEditorAdapter.GetViewAdapter(session.TextView);

			// start a snippet session using in memory xml rather than .xml files
			ExpansionClient.StartExpansion(vsTextView, item);

			// we handled the completion by starting an expansion session so no other handlers should participate
			return CommitResult.Handled;
        }
    }
}