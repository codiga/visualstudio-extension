using System.Collections.Generic;
using System.Linq;

namespace Extension.Rosie.Model.Codiga
{
    /// <summary>
    ///  Represents a ruleset ignore configuration element in the codiga.yml file.
    /// <br/>
    /// This is the element right under the root-level <c>ignore</c> property, e.g.:
    /// <code>
    ///   - my-python-ruleset:
    ///       - rule1:
    ///           - prefix: /path/to/file/to/ignore
    /// </code>
    /// </summary>
    public class RulesetIgnore
    {
        public string RulesetName { get; }

        /// <summary>
        /// Stores [rule name -> rule ignore configuration] mappings.
        /// <br/>
        /// Using a map instead of a <c>List&lt;RuleIgnore></c>, so that we can query the ruleset
        /// configs by name, without having to filter the list by the ruleset name.
        /// </summary>
        public IDictionary<string, RuleIgnore> RuleIgnores { get; } = new Dictionary<string, RuleIgnore>();

        /// <summary>
        /// Saves the ruleset name and the rule ignore configuration from its value.
        /// </summary>
        /// <param name="rulesetName">the ruleset name</param>
        /// <param name="ruleIgnoresConfig">the value associated to the ruleset name property in codiga.yml</param>
        public RulesetIgnore(string rulesetName, object ruleIgnoresConfig)
        {
            RulesetName = rulesetName;

            if (ruleIgnoresConfig is List<object> ruleIgnores)
            {
                foreach (var ruleIgnore in ruleIgnores)
                {
                    var ruleIgn = ruleIgnore switch
                    {
                        /*
                        A rule ignore config can be a single rule name without any prefix value:
                            - rulename
                        */
                        string ruleName => new RuleIgnore(ruleName),
                        
                        /*
                        A rule ignore config can be a Map of the rule name and its object value,
                        with one or more prefix values:
                            - rulename:
                              - prefix: /path/to/file/to/ignore
                            as a {[rulename -> prefix: /path/to/file/to/ignore]} map

                            - rulename2:
                              - prefix:
                                - /path1
                                - /path2
                            as a {[rulename2 -> prefix: /path1, /path2]} map
                        */
                        Dictionary<object, object> ruleIgnoreDict =>
                            new RuleIgnore(
                                ruleIgnoreDict.Keys.FirstOrDefault() as string,
                                ruleIgnoreDict.Values.FirstOrDefault()),
                        _ => null
                    };

                    if (ruleIgn != null)
                        RuleIgnores.Add(ruleIgn.RuleName, ruleIgn);
                }
            }
        }
    }
}