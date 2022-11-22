using System.Collections.Generic;

namespace Extension.Rosie.Model
{
    /// <summary>
    /// The Rosie response object returned by the Codiga API.
    /// </summary>
    public class RosieResponse
    {
        public IList<RosieRuleResponse> RuleResponses { get; set; }
        public IList<string> Errors { get; set; }
    }
}
