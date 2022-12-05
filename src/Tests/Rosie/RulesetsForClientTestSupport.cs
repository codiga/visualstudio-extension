using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using GraphQLClient.Model.Rosie;

namespace Tests.Rosie
{
    /// <summary>
    /// Test utility for creating <see cref="RuleSetsForClient"/>s.
    /// </summary>
    public static class RulesetsForClientTestSupport
    {
        public static readonly Rule PythonRule1 = CreateAstRule(
            10L,
            "python_rule_1",
            "ZnVuY3Rpb24gdmlzaXQocGF0dGVybiwgZmlsZW5hbWUsIGNvZGUpIHsKfQ==",
            "Python");

        public static readonly Rule PythonRule2 = CreateAstRule(
            11L,
            "python_rule_2",
            "ZnVuY3Rpb24gdmlzaXQocGF0dGVybiwgZmlsZW5hbWUsIGNvZGUpIHsKICAgIGFkZEVycm9yKGJ1aWxkRXJyb3IobW9kZS5zdGFydC5saW5lLCBtb2RlLnN0YXJ0LmNvbCwgbW9kZS5lbmQubGluZSwgbW9kZS5lbmQuY29sLCAiZXJyb3IgbWVzc2FnZSIsICJDUklUSUNBTCIsICJzZWN1cml0eSIpKTsKICB9Cn0=",
            "Python");

        public static readonly Rule PythonRule3 = CreateAstRule(
            12L,
            "python_rule_3",
            "ZnVuY3Rpb24gdmlzaXQocGF0dGVybiwgZmlsZW5hbWUsIGNvZGUpIHsKICAgIGNvbnN0IGVycm9yID0gYnVpbGRFcnJvcihtb2RlLnN0YXJ0LmxpbmUsIG1vZGUuc3RhcnQuY29sLCBtb2RlLmVuZC5saW5lLCBtb2RlLmVuZC5jb2wsICJlcnJvciBtZXNzYWdlIiwgIkNSSVRJQ0FMIiwgInNlY3VyaXR5Iik7CiAgICBhZGRFcnJvcihlcnJvcik7CiAgfQp9",
            "Python");

        public static readonly Rule PythonRule4 = CreateAstRule(
            30,
            "python_rule_4",
            "ZnVuY3Rpb24gdmlzaXQocGF0dGVybiwgZmlsZW5hbWUsIGNvZGUpIHsKfQ==",
            "Python");

        public static readonly Rule PythonRule5 = CreateAstRule(
            31,
            "python_rule_5",
            "ZnVuY3Rpb24gdmlzaXQocGF0dGVybiwgZmlsZW5hbWUsIGNvZGUpIHsKICAgIGNvbnN0IGVycm9yID0gYnVpbGRFcnJvcihtb2RlLnN0YXJ0LmxpbmUsIG1vZGUuc3RhcnQuY29sLCBtb2RlLmVuZC5saW5lLCBtb2RlLmVuZC5jb2wsICJlcnJvciBtZXNzYWdlIiwgIkNSSVRJQ0FMIiwgInNlY3VyaXR5Iik7CiAgICBhZGRFcnJvcihlcnJvcik7CiAgfQp9",
            "Python");

        public static readonly Rule JavaRule1 = CreateAstRule(
            20L,
            "java_rule_1",
            "ZnVuY3Rpb24gdmlzaXQocGF0dGVybiwgZmlsZW5hbWUsIGNvZGUpIHsKICAgIGFkZEVycm9yKGJ1aWxkRXJyb3IobW9kZS5zdGFydC5saW5lLCBtb2RlLnN0YXJ0LmNvbCwgbW9kZS5lbmQubGluZSwgbW9kZS5lbmQuY29sLCAiZXJyb3IgbWVzc2FnZSIsICJDUklUSUNBTCIsICJzZWN1cml0eSIpKTsKICB9Cn0=",
            "Java");

        /// <summary>
        /// Returns test rulesets based on the argument ruleset names. Currently, rulesets are returned based on the first
        /// value in the provided list.
        /// </summary>
        public static IReadOnlyCollection<RuleSetsForClient>? GetRulesetsForClient(
            IReadOnlyCollection<string> rulesetNames)
        {
            if (rulesetNames.Count == 0)
                return ImmutableList.Create<RuleSetsForClient>();

            switch (rulesetNames.ToList()[0])
            {
                case "singleRulesetSingleLanguage":
                    return SingleRulesetSingleLanguage(); //Python
                case "singleRulesetMultipleLanguagesDefaultTimestamp":
                case "singleRulesetMultipleLanguages":
                    return SingleRulesetMultipleLanguages(); //Python, Java
                case "multipleRulesetsSingleLanguage":
                    return MultipleRulesetsSingleLanguage(); //Python
                case "multipleRulesetsMultipleLanguages":
                    return MultipleRulesetsMultipleLanguages(); //Python, Java
                case "erroredRuleset":
                    return null;
                default:
                    return ImmutableList.Create<RuleSetsForClient>();
            }
        }

        public static long GetRulesetsLastTimestamp(IReadOnlyCollection<string> rulesetNames)
        {
            return rulesetNames.ToList()[0] switch
            {
                "singleRulesetSingleLanguage" => 101L,
                "singleRulesetMultipleLanguagesDefaultTimestamp" => 100L,
                "singleRulesetMultipleLanguages" => 102L,
                "multipleRulesetsSingleLanguage" => 103L,
                "multipleRulesetsMultipleLanguages" => 104L,
                _ => -1L
            };
        }

        /// <summary>
        /// Returns a single ruleset with a few rules, all configured for the same language.
        /// </summary>
        private static IReadOnlyCollection<RuleSetsForClient> SingleRulesetSingleLanguage()
        {
            var rules = new List<Rule> { PythonRule1, PythonRule2, PythonRule3 };
            var ruleset = new RuleSetsForClient { Id = 1234, Name = "python-ruleset", Rules = rules };
            return ImmutableList.Create(ruleset);
        }

        /// <summary>
        /// Returns a single ruleset with a few rules configured for different languages.
        /// </summary>
        public static IReadOnlyCollection<RuleSetsForClient> SingleRulesetMultipleLanguages()
        {
            var rules = new List<Rule> { PythonRule1, JavaRule1, PythonRule3 };
            var ruleset = new RuleSetsForClient
            {
                Id = 2345, Name = "mixed-ruleset", Rules = rules
            };
            return ImmutableList.Create(ruleset);
        }

        /// <summary>
        /// Returns multiple rulesets with rules all configured for the same language.
        /// </summary>
        private static IReadOnlyCollection<RuleSetsForClient> MultipleRulesetsSingleLanguage()
        {
            var rules = new List<Rule> { PythonRule1, PythonRule2 };
            var rules2 = new List<Rule> { PythonRule3 };

            var ruleset = new RuleSetsForClient { Id = 1234, Name = "python-ruleset", Rules = rules };
            var ruleset2 = new RuleSetsForClient { Id = 6789, Name = "python-ruleset-2", Rules = rules2 };

            return ImmutableList.Create(ruleset, ruleset2);
        }

        /// <summary>
        /// Returns multiple rulesets with rules configured for different languages.
        /// </summary>
        private static IReadOnlyCollection<RuleSetsForClient> MultipleRulesetsMultipleLanguages()
        {
            var rules = new List<Rule> { PythonRule4, JavaRule1 };
            var rules2 = new List<Rule> { PythonRule5 };

            var ruleset = new RuleSetsForClient { Id = 5678, Name = "mixed-ruleset", Rules = rules };
            var ruleset2 = new RuleSetsForClient { Id = 6789, Name = "python-ruleset", Rules = rules2 };

            return ImmutableList.Create(ruleset, ruleset2);
        }

        private static Rule CreateAstRule(long id, string name, string content, string language)
        {
            return new Rule
            {
                Id = id,
                Name = name,
                Content = content,
                RuleType = "Ast",
                Language = language,
                Pattern = null,
                ElementChecked = null
            };
        }
    }
}