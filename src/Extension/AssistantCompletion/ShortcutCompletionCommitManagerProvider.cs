using GraphQLClient;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Extension.AssistantCompletion
{
    [Export(typeof(IAsyncCompletionCommitManagerProvider))]
    [Name("Snippet shortcut commit manager provider")]
    [ContentType("text")]
    class ShortcutCompletionCommitManagerProvider : IAsyncCompletionCommitManagerProvider
    {
        IDictionary<ITextView, IAsyncCompletionCommitManager> cache = new Dictionary<ITextView, IAsyncCompletionCommitManager>();

        [Import]
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService;

        [Import]
        internal ExpansionClient ExpansionClient;

        //[Import]
        //internal CodigaClient CodigaClient;

		public IAsyncCompletionCommitManager GetOrCreate(ITextView textView)
        {
            if (cache.TryGetValue(textView, out var itemSource))
                return itemSource;

            // pass factory service to allow the commit manager to access IVS interfaces through ITextView
            var manager = new ShortcutCompletionCommitManager(EditorAdaptersFactoryService, ExpansionClient, null);

            textView.Closed += (o, e) => cache.Remove(textView); // clean up memory as files are closed
            cache.Add(textView, manager);
            return manager;
        }
    }
}
