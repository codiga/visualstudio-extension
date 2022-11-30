using System.ComponentModel.Composition;
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
    // [TextViewRole(PredefinedTextViewRoles.Analyzable)]
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
                }
            }

            return violationTagger as ITagger<T>;
        }
    }
}
