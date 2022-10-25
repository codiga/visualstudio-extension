using Extension.SnippetFormats;
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
using Extension.Caching;
using Community.VisualStudio.Toolkit;
using System.IO;
using Extension.Logging;

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
			try
			{
				var settings = EditorSettingsProvider.GetCurrentCodigaSettings();

				if (trigger.Character != '.' || !settings.UseCodingAssistant)
				{
					return CompletionStartData.DoesNotParticipateInCompletion;
				}

				var lineStart = triggerLocation.GetContainingLine().Start;
				var spanBeforeCaret = new SnapshotSpan(lineStart, triggerLocation);
				var textBeforeCaret = triggerLocation.Snapshot.GetText(spanBeforeCaret);

				if (!EditorUtils.IsStartOfLine(textBeforeCaret.Substring(0, textBeforeCaret.Length - 1)))
				{
					return CompletionStartData.DoesNotParticipateInCompletion;
				}

				m_triggerLocation = triggerLocation;
				var tokenSpan = FindTokenSpanAtPosition(triggerLocation);
				return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
			}
			catch (Exception e)
			{
                ExtensionLogger.LogException(e);
				return CompletionStartData.DoesNotParticipateInCompletion;
			}
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
            try
            {
				var doc = await VS.Documents.GetActiveDocumentViewAsync();
				var path = doc.Document.FilePath;
				var ext = Path.GetExtension(path);
				var settings = EditorSettingsProvider.GetCurrentIndentationSettings();
				var chachedSnippets = Cache.GetSnippets(LanguageUtils.Parse(ext)).Select(s => SnippetParser.FromCodigaSnippet(s, settings));
				var completionItems = SnippetParser.FromVisualStudioSnippets(chachedSnippets, this);

				return new CompletionContext(completionItems);
			}
            catch (Exception e)
            {
                ExtensionLogger.LogException(e);
                return new CompletionContext(new ImmutableArray<CompletionItem>());
            }
        }

        /// <summary>
        /// Provides detailed snippet information in the tooltip
        /// </summary>
        public async Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
			if (item.Properties.TryGetProperty<VisualStudioSnippet>(nameof(VisualStudioSnippet.CodeSnippet.Snippet), out var snippet))
			{
				return snippet.CodeSnippet.Header.Description;
			}
			return null;
		}
    }
}
