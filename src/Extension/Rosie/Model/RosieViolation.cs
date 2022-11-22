using System;
using System.Collections.Generic;
using System.Linq;

namespace Extension.Rosie.Model
{
    /// <summary>
    /// Represents a code violation found by Rosie.
    /// </summary>
    public class RosieViolation
    {
        public string Message { get; set; }

        /// <summary>
        /// The position of the violation from which the annotation begins.
        /// </summary>
        public RosiePosition Start { get; set; }

        /// <summary>
        /// The position of the violation at which the annotation ends.
        /// </summary>
        public RosiePosition End { get; set; }

        /// <summary>
        /// See constants in <see cref="RosieSeverities"/>.
        /// </summary>
        public string Severity { get; set; }

        public string Category { get; set; }
        public IList<RosieViolationFix> Fixes { get; set; }

        public override bool Equals(object obj)
        {
            return obj is RosieViolation violation &&
                   Message == violation.Message &&
                   EqualityComparer<RosiePosition>.Default.Equals(Start, violation.Start) &&
                   EqualityComparer<RosiePosition>.Default.Equals(End, violation.End) &&
                   Severity == violation.Severity &&
                   Category == violation.Category &&
                   Fixes.SequenceEqual(violation.Fixes);
        }

        public override int GetHashCode()
        {
            int hashCode = 1323978756;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Message);
            hashCode = hashCode * -1521134295 + EqualityComparer<RosiePosition>.Default.GetHashCode(Start);
            hashCode = hashCode * -1521134295 + EqualityComparer<RosiePosition>.Default.GetHashCode(End);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Severity);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Category);
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<RosieViolationFix>>.Default.GetHashCode(Fixes);
            return hashCode;
        }
    }
}