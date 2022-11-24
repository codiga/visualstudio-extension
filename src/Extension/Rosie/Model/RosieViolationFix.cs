using System.Collections.Generic;
using System.Linq;

namespace Extension.Rosie.Model
{
    /// <summary>
    /// Represents a "lightbulb fix" in an editor with one or more potential edits.
    /// <see cref="RosieAnnotationFix"/> works based on the information stored here.
    /// </summary>
    public class RosieViolationFix
    {
        public string Description { get; set; }
        public IList<RosieViolationFixEdit> Edits { get; set; }

        public override bool Equals(object obj)
        {
            return obj is RosieViolationFix fix &&
                   Description == fix.Description &&
                   Edits.SequenceEqual(fix.Edits);
        }

        public override int GetHashCode()
        {
            int hashCode = 202620375;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Description);
            hashCode = hashCode * -1521134295 +
                       EqualityComparer<IList<RosieViolationFixEdit>>.Default.GetHashCode(Edits);
            return hashCode;
        }
    }
}