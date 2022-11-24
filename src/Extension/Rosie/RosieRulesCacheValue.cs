using System.Collections.Generic;
using System.Linq;
using Extension.Rosie.Model;
using GraphQLClient.Model.Rosie;

namespace Extension.Rosie
{
    /// <summary>
    /// Cache value for the <see cref="RosieRulesCache"/>.
    /// </summary>
    public class RosieRulesCacheValue
    {
        /// <summary>
        /// Maps the rule id to [rulesetName, ruleName, RosieRule].
        /// <br/>
        /// This is necessary, so that in <c>RosieApiImpl#GetAnnotations()</c>>} we can pass the rule name and ruleset name,
        /// based on the rule id to <see cref="RosieAnnotation"/>},
        /// therefore we have access to those names in <see cref="AnnotationFixOpenBrowser"/>}.
        /// <br/>
        /// <see cref="RosieRuleResponse"/>} doesn't contain the rule name,
        /// only the rule id, so we have to cache the rule name as well.
        /// </summary>
        public Dictionary<string, RuleWithNames> Rules { get; }

        /// <summary>
        /// Caching <see cref="RosieRule"/> instances, as the number of times the <see cref="RosieRulesCache"/> is updated is much
        /// less than the number of requests sent to the Rosie service. Therefore, only one mapping to
        /// <c>RosieRule</c> instances is performed per cache update, instead of for each Rosie service request.
        /// </summary>
        public IReadOnlyList<RosieRule> RosieRules { get; }

        public RosieRulesCacheValue(IEnumerable<RuleWithNames> rules)
        {
            Rules = rules.ToDictionary(rule => rule.RosieRule.Id, rule => rule);
            RosieRules = Rules.Values.Select(rule => rule.RosieRule).ToList();
        }
    }

    public class RuleWithNames
    {
        public string RulesetName { get; }
        public string? RuleName { get; }
        public RosieRule RosieRule { get; }

        public RuleWithNames(string rulesetName, Rule rule)
        {
            RulesetName = rulesetName;
            RuleName = rule.Name;
            RosieRule = new RosieRule(rulesetName, rule);
        }
    }
}
