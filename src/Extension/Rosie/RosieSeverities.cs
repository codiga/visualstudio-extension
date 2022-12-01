namespace Extension.Rosie
{
    /// <summary>
    /// Severities of Rosie violations.
    /// <br/>
    /// See <c>ruleResponses.violations.severity</c> property on https://doc.codiga.io/docs/rosie/ide-specification/#getting-the-results.
    /// </summary>
    internal static class RosieSeverities
    {
        public const string Critical = "critical";
        public const string Error = "error";
        public const string Warning = "warning";
    }
}