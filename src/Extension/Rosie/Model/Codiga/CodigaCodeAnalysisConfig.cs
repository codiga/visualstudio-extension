using System.Collections.Generic;

namespace Extension.Rosie.Model.Codiga
{
    /// <summary>
    /// Represents a codiga.yml configuration file.
    /// </summary>
    /// <see cref="CodigaConfigFileUtil.DeserializeConfig"/>
    public class CodigaCodeAnalysisConfig
    {
        public static readonly CodigaCodeAnalysisConfig EMPTY = new CodigaCodeAnalysisConfig();

        private List<string>? _rulesets;

        public List<string> Rulesets
        {
            get => _rulesets ?? new List<string>();
            set => _rulesets = value;
        }

        /// <summary>
        /// Stores [ruleset name -> ruleset ignore configuration] mappings.
        /// <br/>
        /// Using a map instead of a <c>List&lt;RulesetIgnore></c>, so that we can query the ruleset
        /// configs by name, without having to filter the list by the ruleset name.
        /// </summary>
        public IDictionary<string, RulesetIgnore> Ignore { get; } = new Dictionary<string, RulesetIgnore>();
    }
}