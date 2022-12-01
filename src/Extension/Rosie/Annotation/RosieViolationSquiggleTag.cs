using Microsoft.VisualStudio.Text.Tagging;

namespace Extension.Rosie.Annotation
{
    /// <summary>
    /// Custom tag implementation for squiggle tagging.
    /// <br/>
    /// This tag is not responsible for carrying information of a violation
    /// returned from the Rosie server, only for providing squiggle information.
    /// For information on violations, <see cref="RosieViolationTag"/> is used.
    /// <br/>
    /// Instances of this class are created by <see cref="RosieViolationSquiggleTagger"/> via <see cref="RosieViolationSquiggleTaggerProvider"/>
    /// </summary>
    public class RosieViolationSquiggleTag : ErrorTag
    {
        public RosieViolationSquiggleTag(string squiggleType, object toolTipContent) : base(squiggleType,
            toolTipContent)
        {
        }
    }
}