using System.Collections.Generic;
using System.Linq;

namespace Extension.Rosie.Model
{
    /// <summary>
    /// Annotation information created by <see cref="DefaultRosieApi"/> based on the
    /// information retrieved in <see cref="RosieResponse"/> from the Codiga API.
    /// </summary>
    /// <seealso cref="RosieAnnotationJetBrains"/>
    public class RosieAnnotation
    {
        public string RulesetName { get; }
        public string RuleName { get; }
        public string Message { get; }
        public string Severity { get; }
        public string Category { get; }
        public RosiePosition Start { get; }
        public RosiePosition End { get; }
        public IList<RosieViolationFix> Fixes { get; }

        public RosieAnnotation(string ruleName, string rulesetName, RosieViolation violation)
        {
            RulesetName = rulesetName;
            RuleName = ruleName;
            Message = violation.Message;
            Severity = violation.Severity;
            Category = violation.Category;
            Start = violation.Start;
            End = violation.End;
            Fixes = violation.Fixes;
        }

        public override bool Equals(object obj)
        {
            return obj is RosieAnnotation annotation &&
                   RulesetName == annotation.RulesetName &&
                   RuleName == annotation.RuleName &&
                   Message == annotation.Message &&
                   Severity == annotation.Severity &&
                   Category == annotation.Category &&
                   EqualityComparer<RosiePosition>.Default.Equals(Start, annotation.Start) &&
                   EqualityComparer<RosiePosition>.Default.Equals(End, annotation.End) &&
                   Fixes.SequenceEqual(annotation.Fixes);
        }

        public override int GetHashCode()
        {
            int hashCode = 638126934;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RulesetName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RuleName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Message);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Severity);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Category);
            hashCode = hashCode * -1521134295 + EqualityComparer<RosiePosition>.Default.GetHashCode(Start);
            hashCode = hashCode * -1521134295 + EqualityComparer<RosiePosition>.Default.GetHashCode(End);
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<RosieViolationFix>>.Default.GetHashCode(Fixes);
            return hashCode;
        }
    }
}