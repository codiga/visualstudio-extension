using System.Collections.Generic;
using System.Linq;

namespace Extension.Rosie
{
    /// <summary>
    /// Stores the ruleset names deserialized from the Codiga config file.
    /// </summary>
    /// <see cref="CodigaConfigFileUtil.DeserializeConfig"/>
    public class CodigaCodeAnalysisConfig
    {
        public List<string>? Rulesets { get; set; }

        /// <summary>
        /// Returns the ruleset names having filtered out null values from them.
        /// <br/>
        /// This is for covering the case where the config file is configured like this:
        /// <code>
        /// rulesets:
        ///   - 
        /// </code>
        /// </summary>
        public List<string>? GetRulesets()
        {
            return Rulesets?.Where(ruleset => ruleset != null).ToList();
        }
    }
}
