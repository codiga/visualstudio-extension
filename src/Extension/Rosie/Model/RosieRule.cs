using GraphQLClient.Model.Rosie;

namespace Extension.Rosie.Model
{
    public class RosieRule
    {
        private const string EntityCheckedFunctionCall = "functioncall";
        private const string EntityCheckedIfCondition = "ifcondition";
        private const string EntityCheckedImport = "import";
        private const string EntityCheckedAssignment = "assign";
        private const string EntityCheckedForLoop = "forloop";
        private const string EntityCheckedFunctionDefinition = "functiondefinition";
        private const string EntityCheckedTryBlock = "tryblock";
        
        public string Id { get; }
        public string ContentBase64 { get; }
        public string Language { get; }
        public string Type { get; }
        public string? EntityChecked { get; }
        public string Pattern { get; }

        public RosieRule(string rulesetName, Rule rule) {
            Id = rulesetName + "/" + rule.Name;
            ContentBase64 = rule.Content;
            Language = rule.Language;
            Type = rule.RuleType;
            EntityChecked = ElementCheckedToRosieEntityChecked(rule.ElementChecked);
            Pattern = rule.Pattern;
        }

        private string? ElementCheckedToRosieEntityChecked(string? elementChecked) {
            return elementChecked switch
            {
                "ForLoop" => EntityCheckedForLoop,
                "Assignment" => EntityCheckedAssignment,
                "FunctionDefinition" => EntityCheckedFunctionDefinition,
                "TryBlock" => EntityCheckedTryBlock,
                "Import" => EntityCheckedImport,
                "IfCondition" => EntityCheckedIfCondition,
                "FunctionCall" => EntityCheckedFunctionCall,
                _ => null
            };
        }
    }
}