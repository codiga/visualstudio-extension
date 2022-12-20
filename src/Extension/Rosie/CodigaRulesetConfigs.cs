namespace Extension.Rosie
{
    /// <summary>
    /// Provides default ruleset configurations for the Codiga config file.
    /// </summary>
    public static class CodigaRulesetConfigs
    {
        public const string DefaultPythonRulesetConfig =
            "rulesets:\n" +
            "  - python-security\n" +
            "  - python-best-practices\n" +
            "  - python-code-style";
    }
}