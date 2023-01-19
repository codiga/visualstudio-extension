using System.Collections.Generic;
using System.Linq;

namespace Extension.Rosie.Model.Codiga
{
    /// <summary>
    /// Represents a rule ignore configuration element in the codiga.yml file.
    /// <br/>
    /// This is the element right under a ruleset name property, e.g.:
    /// <code>
    ///       - rule1:
    ///           - prefix: /path/to/file/to/ignore
    /// </code>
    /// or
    /// <code>
    ///       - rule2:
    ///           - prefix:
    ///               - /path1
    ///               - /path2
    /// </code>
    /// </summary>
    public class RuleIgnore
    {
        public string RuleName { get; }

        /// <summary>
        /// The list of prefix values under the <c>prefix</c> property.
        /// <br/>
        /// In case multiple <c>prefix</c> properties are defined under the same rule config,
        /// they are all added to this list.
        /// <br/>
        /// For example, in case of:
        /// <code>
        /// ignore:
        ///   - my-python-ruleset:
        ///     - rule1:
        ///       - prefix:
        ///         - /path1
        ///         - /path2
        ///       - prefix: /path3
        /// </code>
        /// all of <c>/path1</c>, <c>/path2</c> and <c>/path3</c> are stored here.
        /// <br/>
        /// In case a <c>prefix</c> property contains the same value multiple times,
        /// they are deduplicated and only once instance is stored, for example:
        /// <code>
        /// ignore:
        ///   - my-python-ruleset:
        ///     - rule1:
        ///       - prefix:
        ///         - /path1
        ///         - /path1
        /// </code>
        /// </summary>
        public List<string> Prefixes { get; } = new List<string>();

        public RuleIgnore(string ruleName)
        {
            RuleName = ruleName;
        }

        public RuleIgnore(string ruleName, object ruleIgnore)
        {
            RuleName = ruleName;
            if (ruleIgnore is List<object> prefixIgnores)
            {
                foreach (var prefixIgnore in prefixIgnores)
                {
                    if (prefixIgnore is Dictionary<object, object> prefixIgnoreDict)
                    {
                        var prefixIgnoreValue = prefixIgnoreDict.Values.FirstOrDefault();
                        /*
                            A 'prefix' property can have a single String value:
                                 - prefix: /path/to/file/to/ignore
                        */
                        if (prefixIgnoreValue is string value)
                            Prefixes = new List<string> { value };
                        
                        /*
                            A 'prefix' property can also have multiple String values as a list:
                                - prefix:
                                  - /path1
                                  - /path2
                        */
                        else if (prefixIgnoreValue is List<object> prefixes)
                            //It filters out null and non-String prefix values
                            Prefixes = prefixes
                                .Where(prefix => prefix != null)
                                .OfType<string>()
                                .Distinct()
                                .ToList();
                    }
                }
            }
        }
    }
}
