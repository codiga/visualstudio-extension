using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Extension.Rosie.Annotation
{
    /// <summary>
    /// Creates <see cref="RosieViolationTagger"/> instances for <see cref="ITextView"/> and <see cref="ITextBuffer"/>
    /// pairs, that in turn can create <see cref="RosieViolationTag"/>s.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("any")]
    [TagType(typeof(RosieViolationTag))]
    //Restricts the creation of this provider to certain text view roles
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal class RosieViolationTaggerProvider : IViewTaggerProvider
    {
        /// <summary>
        /// Creates a <see cref="RosieViolationTagger"/> for the specified view and buffer.
        /// <br/>
        /// The tagger instance is saved in the <c>textView</c>'s Properties, thus, if there is
        /// such tagger instance already created, we don't create another one, but return the cached tagger instance.
        /// <br/>
        /// A <c>RosieViolationTagger</c> is created only when the file being tagged is actually supported by Rosie. 
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            // This provider is only interested in creating tagging for the top buffer,
            // and for IErrorTags, so that we can filter out, among others, ones for RosieViolationSquiggleTags.
            if (textView.TextBuffer != buffer || typeof(T) == typeof(IErrorTag))
                return null;

            if (!textView.Properties.TryGetProperty(typeof(RosieViolationTagger),
                    out RosieViolationTagger violationTagger))
            {
                //Create a tagger only when the language of the current file is supported by Rosie
                if (RosieClient.IsLanguageOfFileSupported(buffer.GetFileName()))
                {
                    violationTagger = new RosieViolationTagger(textView, buffer);
                    textView.Properties[typeof(RosieViolationTagger)] = violationTagger;
                    textView.Properties[RosieRulesCache.CacheLastUpdatedTimeStampProp] = -1L;

                    /*
                     * Called when the view gets focus, and updates the tagging in that view if the rules cache has
                     * changed since the last tagging of that text view.
                     * This is complementary to the logic that updates the tagging in the the active view after the cache changed.
                     * For that see RosieRulesCache.NotifyActiveDocumentForTagUpdateAsync().
                     *
                     * The last cache change stamp is stored in the Properties of ITextView in a property named 'CacheLastUpdatedTimeStamp'.
                     */
                    textView.GotAggregateFocus += (sender, args) =>
                    {
                        if (textView.Properties.ContainsProperty(RosieRulesCache.CacheLastUpdatedTimeStampProp) &&
                            RosieRulesCache.Instance != null &&
                            RosieRulesCache.Instance.CacheLastUpdatedTimeStamp !=
                            (long)textView.Properties[RosieRulesCache.CacheLastUpdatedTimeStampProp])
                        {
                            ThreadHelper.JoinableTaskFactory.Run(async () =>
                                await violationTagger.UpdateAnnotationsAndNotifyTagsChangedAsync(textView));
                        }
                    };
                }
            }

            return violationTagger as ITagger<T>;
        }
    }
}