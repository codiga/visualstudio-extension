namespace Extension.Rosie.Model
{
    /// <summary>
    /// Provides values and mapping logic for rule AST types.
    /// </summary>
    internal static class RosieRuleAstTypes
    {
        private const string EntityCheckedFunctionCall = "functioncall";
        private const string EntityCheckedIfCondition = "ifcondition";
        private const string EntityCheckedImport = "import";
        private const string EntityCheckedAssignment = "assign";
        private const string EntityCheckedForLoop = "forloop";
        private const string EntityCheckedFunctionDefinition = "functiondefinition";
        private const string EntityCheckedTryBlock = "tryblock";
        private const string EntityCheckedType = "type";
        private const string EntityCheckedInterface = "interface";
        private const string EntityCheckedHtmlElement = "htmlelement";
        private const string EntityCheckedClassDefinition = "classdefinition";
        private const string EntityCheckedFunctionExpression = "functionexpression";
        
        /// <summary>
        /// Maps the argument element checked to its Rosie counterpart value.
        /// </summary>
        internal static string? ElementCheckedToRosieEntityChecked(string? elementChecked) {
            if (elementChecked == null) {
                return null;
            }
            
            return elementChecked switch
            {
                "ForLoop" => EntityCheckedForLoop,
                "Assignment" => EntityCheckedAssignment,
                "FunctionDefinition" => EntityCheckedFunctionDefinition,
                "TryBlock" => EntityCheckedTryBlock,
                "Import" => EntityCheckedImport,
                "IfCondition" => EntityCheckedIfCondition,
                "FunctionCall" => EntityCheckedFunctionCall,
                "Type" => EntityCheckedType,
                "Interface" => EntityCheckedInterface,
                "HtmlElement" => EntityCheckedHtmlElement,
                "ClassDefinition" => EntityCheckedClassDefinition,
                "FunctionExpression" => EntityCheckedFunctionExpression,
                _ => null
            };
        }
    }
}