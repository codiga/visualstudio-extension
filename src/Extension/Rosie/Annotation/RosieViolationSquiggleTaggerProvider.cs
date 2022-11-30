using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Extension.Rosie.Annotation
{
    /// <summary>
    /// Creates <see cref="RosieViolationSquiggleTagger"/> instances for <see cref="ITextView"/> and <see cref="ITextBuffer"/>
    /// pairs, that in turn can create <see cref="RosieViolationSquiggleTag"/>s.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("any")]
    [TagType(typeof(RosieViolationSquiggleTag))]
    //Restricts the creation of this provider to certain text view roles
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal class RosieViolationSquiggleTaggerProvider : IViewTaggerProvider
    {
        [Import] internal IViewTagAggregatorFactoryService TagAggregatorFactory { get; set; }

        /// <summary>
        /// Creates a <see cref="RosieViolationSquiggleTagger"/> for the specified view and buffer.
        /// <br/>
        /// The tagger instance is saved in the <c>textView</c>'s Properties, thus, if there is
        /// such tagger instance already created, we don't create another one, but return the cached tagger instance.
        /// <br/>
        /// A <c>RosieViolationSquiggleTagger</c> is created only when the file being tagged is actually supported by Rosie. 
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            // This provider is only interested in creating tagging for the top buffer,
            // and not for IErrorTags, so that we can filter out, among others, ones for RosieViolationTags.
            if (buffer != textView.TextBuffer || typeof(T) != typeof(IErrorTag))
                return null;

            if (!textView.Properties.TryGetProperty(typeof(RosieViolationSquiggleTagger),
                    out RosieViolationSquiggleTagger squiggleTagger))
            {
                //Create a tagger only when the language of the current file is supported by Rosie
                if (RosieClient.IsLanguageOfFileSupported(buffer.GetFileName()))
                {
                    squiggleTagger = new RosieViolationSquiggleTagger(buffer,
                        TagAggregatorFactory.CreateTagAggregator<RosieViolationTag>(textView));
                    textView.Properties[typeof(RosieViolationSquiggleTagger)] = squiggleTagger;
                }
            }

            return squiggleTagger as ITagger<T>;
        }
    }
}
