using System.Collections.Generic;

namespace Extension.Rosie.Model
{
    public class RosieRuleResponse
    {
        public string Identifier { get; set; }
        public IList<RosieViolation> Violations { get; set; }
        public IList<string> Errors { get; set; }
        public string? ExecutionError { get; set; }
        public string? Output { get; set; }
    }
}