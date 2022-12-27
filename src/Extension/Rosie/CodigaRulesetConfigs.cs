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

        public const string DefaultJavascriptRulesetConfig =
            "rulesets:\n" +
            "  - jsx-a11y\n" +
            "  - jsx-react\n" +
            "  - react-best-practices";
    }
}