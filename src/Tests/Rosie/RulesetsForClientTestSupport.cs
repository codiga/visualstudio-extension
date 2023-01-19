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
        // Python rules
        
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
        
        // Java rules

        public static readonly Rule JavaRule1 = CreateAstRule(
            20L,
            "java_rule_1",
            "ZnVuY3Rpb24gdmlzaXQocGF0dGVybiwgZmlsZW5hbWUsIGNvZGUpIHsKICAgIGFkZEVycm9yKGJ1aWxkRXJyb3IobW9kZS5zdGFydC5saW5lLCBtb2RlLnN0YXJ0LmNvbCwgbW9kZS5lbmQubGluZSwgbW9kZS5lbmQuY29sLCAiZXJyb3IgbWVzc2FnZSIsICJDUklUSUNBTCIsICJzZWN1cml0eSIpKTsKICB9Cn0=",
            "Java");
        
        // JavaScript rules
        
        private static readonly Rule JavaScriptRule1 = CreateAstRule(
            10L,
            "javascript_rule_1",
            "ZnVuY3Rpb24gdmlzaXQocGF0dGVybiwgZmlsZW5hbWUsIGNvZGUpIHsKfQ==",
            "Javascript");

        private static readonly Rule JavaScriptRule2 = CreateAstRule(
            11L,
            "javascript_rule_2",
            "ZnVuY3Rpb24gdmlzaXQocGF0dGVybiwgZmlsZW5hbWUsIGNvZGUpIHsKICAgIGFkZEVycm9yKGJ1aWxkRXJyb3IobW9kZS5zdGFydC5saW5lLCBtb2RlLnN0YXJ0LmNvbCwgbW9kZS5lbmQubGluZSwgbW9kZS5lbmQuY29sLCAiZXJyb3IgbWVzc2FnZSIsICJDUklUSUNBTCIsICJzZWN1cml0eSIpKTsKICB9Cn0=",
            "Javascript");

        private static readonly Rule JavaScriptRule3 = CreateAstRule(
            12L,
            "javascript_rule_3",
            "ZnVuY3Rpb24gdmlzaXQocGF0dGVybiwgZmlsZW5hbWUsIGNvZGUpIHsKICAgIGNvbnN0IGVycm9yID0gYnVpbGRFcnJvcihtb2RlLnN0YXJ0LmxpbmUsIG1vZGUuc3RhcnQuY29sLCBtb2RlLmVuZC5saW5lLCBtb2RlLmVuZC5jb2wsICJlcnJvciBtZXNzYWdlIiwgIkNSSVRJQ0FMIiwgInNlY3VyaXR5Iik7CiAgICBhZGRFcnJvcihlcnJvcik7CiAgfQp9",
            "Javascript");

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
                case "single-ruleset-single-language":
                    return SingleRulesetSingleLanguage(); //Python
                case "single-set-multi-lang-def-ts":
                case "single-ruleset-multi-languages":
                    return SingleRulesetMultipleLanguages(); //Python, Java
                case "multi-rulesets-single-language":
                    return MultipleRulesetsSingleLanguage(); //Python
                case "multi-rulesets-multi-languages":
                    return MultipleRulesetsMultipleLanguages(); //Python, Java
                case "errored-ruleset":
                    return null;
                case "javascript-ruleset":
                    return JavascriptRulesets();
                default:
                    return ImmutableList.Create<RuleSetsForClient>();
            }
        }

        public static long GetRulesetsLastTimestamp(IReadOnlyCollection<string> rulesetNames)
        {
            return rulesetNames.ToList()[0] switch
            {
                "single-ruleset-single-language" => 101L,
                "single-set-multi-lang-def-ts" => 100L,
                "single-ruleset-multi-languages" => 102L,
                "multi-rulesets-single-language" => 103L,
                "multi-rulesets-multi-languages" => 104L,
                "javascript-ruleset" => 105L,
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
        
        /// <summary>
        /// Returns a single ruleset with JavaScript rules.
        /// </summary>
        private static IReadOnlyCollection<RuleSetsForClient> JavascriptRulesets() {
            var rules = new List<Rule> { JavaScriptRule1, JavaScriptRule2, JavaScriptRule3};

            var ruleset = new RuleSetsForClient { Id = 5678, Name = "javascript-ruleset", Rules = rules };

            return ImmutableList.Create(ruleset);
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