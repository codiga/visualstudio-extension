using Extension.Xml;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Extension.AssistantCompletion
{
    class ShortcutCompletionSource : IAsyncCompletionSource
    {
        private SnippetCache Cache { get; }
        private ITextStructureNavigatorSelectorService StructureNavigatorSelector { get; }

        private SnapshotPoint m_triggerLocation;

        public ShortcutCompletionSource(SnippetCache cache, ITextStructureNavigatorSelectorService structureNavigatorSelector)
        {
            Cache = cache;
            StructureNavigatorSelector = structureNavigatorSelector;
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            if (!char.IsPunctuation(trigger.Character))
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            //We don't trigger completion when user typed
            //if (char.IsNumber(trigger.Character)         // a number
            //    || char.IsPunctuation(trigger.Character) // punctuation
            //    || trigger.Character == '\n'             // new line
            //    || trigger.Reason == CompletionTriggerReason.Backspace
            //    || trigger.Reason == CompletionTriggerReason.Deletion)
            //{
            //    return CompletionStartData.DoesNotParticipateInCompletion;
            //}

            // We participate in completion and provide the "applicable to span".
            // This span is used:
            // 1. To search (filter) the list of all completion items
            // 2. To highlight (bold) the matching part of the completion items
            // 3. In standard cases, it is replaced by content of completion item upon commit.

            // If you want to extend a language which already has completion, don't provide a span, e.g.
            // return CompletionStartData.ParticipatesInCompletionIfAny

            // If you provide a language, but don't have any items available at this location,
            // consider providing a span for extenders who can't parse the codem e.g.
            // return CompletionStartData(CompletionParticipation.DoesNotProvideItems, spanForOtherExtensions);
            m_triggerLocation = triggerLocation;
            var tokenSpan = FindTokenSpanAtPosition(triggerLocation);
            return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
        }

        private SnapshotSpan FindTokenSpanAtPosition(SnapshotPoint triggerLocation)
        {
            // This method is not really related to completion,
            // we mostly work with the default implementation of ITextStructureNavigator 
            // You will likely use the parser of your language
            ITextStructureNavigator navigator = StructureNavigatorSelector.GetTextStructureNavigator(triggerLocation.Snapshot.TextBuffer);
            TextExtent extent = navigator.GetExtentOfWord(triggerLocation);
            if (triggerLocation.Position > 0 && (!extent.IsSignificant || !extent.Span.GetText().Any(c => char.IsLetterOrDigit(c))))
            {
                // Improves span detection over the default ITextStructureNavigation result
                extent = navigator.GetExtentOfWord(triggerLocation - 1);
            }

            var tokenSpan = triggerLocation.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);

            var snapshot = triggerLocation.Snapshot;
            var tokenText = tokenSpan.GetText(snapshot);
            if (string.IsNullOrWhiteSpace(tokenText))
            {
                // The token at this location is empty. Return an empty span, which will grow as user types.
                return new SnapshotSpan(triggerLocation, 0);
            }

            // Trim quotes and new line characters.
            int startOffset = 0;
            int endOffset = 0;

            if (tokenText.Length > 0)
            {
                if (tokenText.StartsWith("\""))
                    startOffset = 1;
            }
            if (tokenText.Length - startOffset > 0)
            {
                if (tokenText.EndsWith("\"\r\n"))
                    endOffset = 3;
                else if (tokenText.EndsWith("\r\n"))
                    endOffset = 2;
                else if (tokenText.EndsWith("\"\n"))
                    endOffset = 2;
                else if (tokenText.EndsWith("\n"))
                    endOffset = 1;
                else if (tokenText.EndsWith("\""))
                    endOffset = 1;
            }

            return new SnapshotSpan(tokenSpan.GetStartPoint(snapshot) + startOffset, tokenSpan.GetEndPoint(snapshot) - endOffset);
        }

        public async Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
        {
            // See whether we are in the key or value portion of the pair
            var lineStart = triggerLocation.GetContainingLine().Start;
            var spanBeforeCaret = new SnapshotSpan(lineStart, triggerLocation);
            var textBeforeCaret = triggerLocation.Snapshot.GetText(spanBeforeCaret);

            return null;
        }

		/// <summary>
		/// Builds a <see cref="CompletionItem"/> based on <see cref="VisualStudioSnippet"/>
		/// </summary>
		private CompletionItem MakeItemFromSnippet(VisualStudioSnippet snippet)
        {
            ImageElement icon = null;
            ImmutableArray<CompletionFilter> filters;

            var item = new CompletionItem(snippet.CodeSnippet.Header.Shortcut, this);
    //            displayText: snippet.CodeSnippet.Header.Shortcut,
    //            source: this,
    //            icon: icon,
    //            filters: filters,
    //            suffix: element.Symbol,
    //            insertText: element.Name,
    //            sortText: $"Element {element.AtomicNumber,3}",
    //            filterText: $"{element.Name} {element.Symbol}",
				//automationText: element.Name,
				//attributeIcons: ImmutableArray<ImageElement>.Empty,
    //            commitCharacters: new char[] { ' ', '\'', '"', ',', '.', ';', ':' }.ToImmutableArray(),
    //            applicableToSpan: FindTokenSpanAtPosition(m_triggerLocation),
    //            isCommittedAsSnippet: true,
    //            isPreselected: false);

			// Each completion item we build has a reference to the element in the property bag.
			// We use this information when we construct the tooltip.
			//item.Properties.AddProperty(nameof(ElementCatalog.Element), element);

            return item;
        }

        /// <summary>
        /// Provides detailed snippet information in the tooltip
        /// </summary>
        public async Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
            return null;
        }
    }
}
