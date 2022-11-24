using System.Collections.Generic;

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
    }
}