using System.Collections.Generic;

namespace Extension.Rosie
{
    /// <summary>
    /// Stores the ruleset names deserialized from the Codiga config file.
    /// </summary>
    /// <see cref="CodigaConfigFileUtil.DeserializeConfig"/>
    public class CodigaCodeAnalysisConfig
    {
        public List<string>? Rulesets { get; set; }
    }
}
