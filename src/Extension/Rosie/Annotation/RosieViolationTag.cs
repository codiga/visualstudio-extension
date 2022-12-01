using Extension.Rosie.Model;
using Microsoft.VisualStudio.Text.Tagging;

namespace Extension.Rosie.Annotation
{
    /// <summary>
    /// Custom tag implementation for text tagging, that also stores a <see cref="RosieAnnotation"/> instance
    /// to be used in code analysis and related lightbulb actions.
    /// <br/>
    /// This tag is not user-visible, it tags a span of text in a text buffer behind the scenes.
    /// <br/>
    /// It is not responsible for providing squiggle information, only for carrying information of a violation
    /// returned from the Rosie server. The squiggle information is provided by <see cref="RosieViolationSquiggleTag"/>.
    /// <br/>
    /// Instances of this class are created by <see cref="RosieViolationTagger"/> via <see cref="RosieViolationTaggerProvider"/>
    /// </summary>
    public class RosieViolationTag : ITag
    {
        public RosieAnnotation Annotation { get; }

        public RosieViolationTag(RosieAnnotation annotation)
        {
            Annotation = annotation;
        }
    }
}