namespace Extension.Rosie
{
    /// <summary>
    /// Edit types for Rosie violations.
    /// <br/>
    /// See <c>ruleResponses.violations.fixes.edits.editType</c> property on https://doc.codiga.io/docs/rosie/ide-specification/#getting-the-results.
    /// </summary>
    internal static class RosieEditTypes
    {
        public const string Add = "add";
        public const string Update = "update";
        public const string Remove = "remove";
    }
}