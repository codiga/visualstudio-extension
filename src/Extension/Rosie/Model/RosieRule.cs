using GraphQLClient.Model.Rosie;

namespace Extension.Rosie.Model
{
    public class RosieRule
    {
        private const string ENTITY_CHECKED_FUNCTION_CALL = "functioncall";
        private const string ENTITY_CHECKED_IF_CONDITION = "ifcondition";
        private const string ENTITY_CHECKED_IMPORT = "import";
        private const string ENTITY_CHECKED_ASSIGNMENT = "assign";
        private const string ENTITY_CHECKED_FOR_LOOP = "forloop";
        private const string ENTITY_CHECKED_FUNCTION_DEFINITION = "functiondefinition";
        private const string ENTITY_CHECKED_TRY_BLOCK = "tryblock";
        
        public string Id { get; }
        public string ContentBase64 { get; }
        public string Language { get; }
        public string Type { get; }
        public string? EntityChecked { get; }
        public string Pattern { get; }
        
        private string? ElementCheckedToRosieEntityChecked(string? elementChecked) {
            // if (elementChecked == null) {
            //     return null;
            // }

            return elementChecked switch
            {
                "ForLoop" => ENTITY_CHECKED_FOR_LOOP,
                "Assignment" => ENTITY_CHECKED_ASSIGNMENT,
                "FunctionDefinition" => ENTITY_CHECKED_FUNCTION_DEFINITION,
                "TryBlock" => ENTITY_CHECKED_TRY_BLOCK,
                "Import" => ENTITY_CHECKED_IMPORT,
                "IfCondition" => ENTITY_CHECKED_IF_CONDITION,
                "FunctionCall" => ENTITY_CHECKED_FUNCTION_CALL,
                _ => null
            };
        }

        public RosieRule(string rulesetName, Rule rule) {
            Id = rulesetName + "/" + rule.Name;
            ContentBase64 = rule.Content;
            Language = rule.Language;
            Type = rule.RuleType;
            EntityChecked = ElementCheckedToRosieEntityChecked(rule.ElementChecked);
            Pattern = rule.Pattern;
        }
    }
}