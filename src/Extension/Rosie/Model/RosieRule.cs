using GraphQLClient.Model.Rosie;

namespace Extension.Rosie.Model
{
    public class RosieRule
    {
        public string RulesetName { get; }
        public string RuleName { get; }
        public string Id { get; }
        public string ContentBase64 { get; }
        public string Language { get; }
        public string Type { get; }
        public string? EntityChecked { get; }
        public string Pattern { get; }

        public RosieRule(string rulesetName, Rule rule) {
            RulesetName = rulesetName;
            RuleName = rule.Name;
            Id = rulesetName + "/" + rule.Name;
            ContentBase64 = rule.Content;
            Language = rule.Language;
            Type = rule.RuleType;
            EntityChecked = RosieRuleAstTypes.ElementCheckedToRosieEntityChecked(rule.ElementChecked);
            Pattern = rule.Pattern;
        }
    }
}