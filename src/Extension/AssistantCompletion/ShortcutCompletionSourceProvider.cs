using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extension.Caching;
using System.Windows.Controls;

namespace Extension.AssistantCompletion
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("Snippet shortcut completion provider")]
    [ContentType("text")]
    class ShortcutCompletionSourceProvider : IAsyncCompletionSourceProvider
	{
        IDictionary<ITextView, IAsyncCompletionSource> cache = new Dictionary<ITextView, IAsyncCompletionSource>();

        [Import]
		SnippetCache Cache;

        [Import]
        ITextStructureNavigatorSelectorService StructureNavigatorSelector;

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            if (cache.TryGetValue(textView, out var itemSource))
                return itemSource;
            
            var source = new ShortcutCompletionSource(Cache, StructureNavigatorSelector); // opportunity to pass in MEF parts
            textView.Closed += (o, e) => cache.Remove(textView); // clean up memory as files are closed
            cache.Add(textView, source);
            return source;
        }
    }
}
