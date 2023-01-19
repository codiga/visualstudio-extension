using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Extension.Caching;
using Extension.Rosie;
using Extension.Rosie.Model;
using Extension.Rosie.Model.Codiga;
using GraphQLClient;
using Moq;
using NUnit.Framework;
using LanguageEnumeration = Extension.SnippetFormats.LanguageUtils.LanguageEnumeration;

namespace Tests.Rosie
{
    /// <summary>
    /// Unit test for <see cref="RosieRulesCache"/>.
    /// </summary>
    /// <seealso cref="https://www.codeproject.com/Articles/5286617/Moq-and-out-Parameter"/>
    /// <seealso cref="https://stackoverflow.com/questions/1068095/assigning-out-ref-parameters-in-moq"/>
    [TestFixture]
    internal class RosieRulesCacheTest
    {
        private Mock<Solution>? _solution;
        private string? _solutionDirPath;
        private string? _codigaConfigFile;
        private ICodigaClientProvider _clientProvider;
        private RosieRulesCache? _cache;
        private string? _pythonFile;
        private string? _javaScriptFile;

        /// <summary>
        /// Initializes the test with a Solution directory with a mock Solution,
        /// and a codiga.yml in that directory,
        /// </summary>
        [SetUp]
        public void Setup()
        {
            //This makes sure that when executing the whole test suite, these tests start with a clean slate,
            //even when other tests use and initialize the RosieRulesCache.
            if (RosieRulesCache.Instance != null)
                RosieRulesCache.Dispose();

            _solutionDirPath = Path.GetTempPath();
            _codigaConfigFile = $"{_solutionDirPath}codiga.yml";
            _pythonFile = $"{_solutionDirPath}python_file.py";
            _javaScriptFile = $"{_solutionDirPath}javascript_file.js";

            _solution = new Mock<Solution>();
            //Necessary to mock for CodigaConfigFileUtil.FindCodigaConfigFile()
            _solution.Setup(s => s.FullName).Returns(_solutionDirPath);
        }

        #region HandleCacheUpdateAsync

        [Test]
        public async Task HandleCacheUpdateAsync_should_return_no_codiga_client_for_missing_codiga_client()
        {
            RosieRulesCache.Initialize(null, new NoCodigaClientProvider());
            var updateResult = await RosieRulesCache.Instance.HandleCacheUpdateAsync();

            Assert.AreEqual(updateResult, RosieRulesCache.UpdateResult.NoCodigaClient);
        }

        [Test]
        public async Task HandleCacheUpdateAsync_should_clear_cache_and_return_no_config_file_for_missing_config_file()
        {
            //Configure a Codiga config file, and initialize the cache with some rules

            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");

            Assert.That(RosieRulesCache.IsInitializedWithRules, Is.False);

            await _cache.HandleCacheUpdateAsync();

            //Delete the Codiga config file and try to update the cache

            File.Delete(_codigaConfigFile);

            var updateResult = await _cache.HandleCacheUpdateAsync();

            Assert.AreEqual(updateResult, RosieRulesCache.UpdateResult.NoConfigFile);
            
            var rules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            
            Assert.Multiple(() =>
            {
                Assert.That(_cache.CodigaConfig.Rulesets, Is.Empty);
                Assert.That(rules, Is.Empty);
                Assert.That(RosieRulesCache.IsInitializedWithRules, Is.True);
            });
        }

        //Handles the UpdateCacheFromModifiedCodigaConfigFile branch
        [Test]
        public async Task HandleCacheUpdateAsync_should_populate_empty_cache_from_codiga_config_file()
        {
            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");

            var updateResult = await _cache.HandleCacheUpdateAsync();

            Assert.AreEqual(updateResult, RosieRulesCache.UpdateResult.Success);
            
            var rules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            
            Assert.Multiple(() =>
            {
                Assert.That(rules[0].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
                Assert.That(rules[1].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
                Assert.That(rules[2].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule3.Name}"));
            });
        }

        //Handles the UpdateCacheFromModifiedCodigaConfigFile branch
        [Test]
        public async Task HandleCacheUpdateAsync_should_update_non_empty_cache_from_codiga_config_file()
        {
            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);

            Assert.That(rules, Has.Count.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(rules[0].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
                Assert.That(rules[1].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
                Assert.That(rules[2].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule3.Name}"));
            });

            await UpdateCodigaConfig(@"
rulesets:
  - multi-rulesets-single-language");

            var updateResult = await _cache.HandleCacheUpdateAsync();

            Assert.AreEqual(updateResult, RosieRulesCache.UpdateResult.Success);

            Assert.That(rules, Has.Count.EqualTo(3));
            
            var rulesMulti = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.Multiple(() =>
            {
                Assert.That(rulesMulti[0].Id,
                    Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
                Assert.That(rulesMulti[1].Id,
                    Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
                Assert.That(rulesMulti[2].Id,
                    Is.EqualTo($"python-ruleset-2/{RulesetsForClientTestSupport.PythonRule3.Name}"));
            });
        }

        //Handles the UpdateCacheFromChangesOnServer branch
        [Test]
        public async Task HandleCacheUpdateAsync_should_update_non_empty_cache_from_server()
        {
            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");

            _cache.UpdateCacheFrom(RulesetsForClientTestSupport.SingleRulesetMultipleLanguages());
            _cache.RulesetslastUpdatedTimeStamp = 102L;
            _cache.ConfigFileLastWriteTime = File.GetLastWriteTime(_codigaConfigFile);
            _cache.CodigaConfig = new CodigaCodeAnalysisConfig
            {
                Rulesets = new List<string> { "single-ruleset-single-language" }
            };

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.That(rules.Count, Is.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(102L));

            await _cache.HandleCacheUpdateAsync();

            var updatedRules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.That(updatedRules.Count, Is.EqualTo(3));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(101L));
        }

        #endregion

        #region UpdateCacheFromModifiedCodigaConfigFileAsync

        [Test]
        public async Task
            UpdateCacheFromModifiedCodigaConfigFileAsync_should_clear_cache_for_null_deserialization_result()
        {
            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");

            await _cache.HandleCacheUpdateAsync();

            Assert.IsFalse(_cache.IsEmpty());

            await UpdateCodigaConfig(@"rulesets:");

            await _cache.HandleCacheUpdateAsync();

            Assert.IsTrue(_cache.IsEmpty());
        }

        [Test]
        public async Task
            UpdateCacheFromModifiedCodigaConfigFileAsync_should_clear_cache_for_no_rulesets_in_config_file()
        {
            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");

            await _cache.HandleCacheUpdateAsync();

            Assert.IsFalse(_cache.IsEmpty());

            await UpdateCodigaConfig(@"
rulesets:
  - ");

            await _cache.HandleCacheUpdateAsync();

            Assert.IsTrue(_cache.IsEmpty());
        }

        [Test]
        public async Task
            UpdateCacheFromModifiedCodigaConfigFileAsync_should_not_update_for_null_rulesets_returned_from_server()
        {
            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.Multiple(() =>
            {
                Assert.That(rules[0].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
                Assert.That(rules[1].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
                Assert.That(rules[2].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule3.Name}"));
            });

            await UpdateCodigaConfig(@"
rulesets:
  - errored-ruleset");

            await _cache.HandleCacheUpdateAsync();

            var updatedRules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.Multiple(() =>
            {
                Assert.That(updatedRules[0].Id,
                    Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
                Assert.That(updatedRules[1].Id,
                    Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
                Assert.That(updatedRules[2].Id,
                    Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule3.Name}"));
            });
        }

        [Test]
        public async Task
            UpdateCacheFromModifiedCodigaConfigFileAsync_should_clear_cache_for_no_rulesets_returned_from_server()
        {
            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");

            await _cache.HandleCacheUpdateAsync();

            Assert.IsFalse(_cache.IsEmpty());

            await UpdateCodigaConfig(@"
rulesets:
  - non-existent-ruleset");

            await _cache.HandleCacheUpdateAsync();

            Assert.IsTrue(_cache.IsEmpty());
        }

        [Test]
        public async Task
            UpdateCacheFromModifiedCodigaConfigFileAsync_should_update_cache_for_same_last_updated_timestamp()
        {
            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");

            await _cache.HandleCacheUpdateAsync();
            _cache.RulesetslastUpdatedTimeStamp = 100L;

            await UpdateCodigaConfig(@"
rulesets:
  - single-set-multi-lang-def-ts");

            await _cache.HandleCacheUpdateAsync();

            var updatedRules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.That(updatedRules, Has.Count.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(100L));
        }

        [Test]
        public async Task
            UpdateCacheFromModifiedCodigaConfigFileAsync_should_update_cache_for_different_last_updated_timestamp()
        {
            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");

            await _cache.HandleCacheUpdateAsync();
            _cache.RulesetslastUpdatedTimeStamp = 100L;

            await UpdateCodigaConfig(@"
rulesets:
  - single-ruleset-multi-languages");

            await _cache.HandleCacheUpdateAsync();

            var updatedRules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.That(updatedRules, Has.Count.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(102L));
        }

        #endregion

        #region UpdateCacheFromChangesOnServerAsync

        [Test]
        public async Task UpdateCacheFromChangesOnServerAsync_should_not_update_cache_for_no_ruleset_name()
        {
            InitConfigAndCache(@"
rulesets:
  - ");

            //Updates for "change" in the config file
            await _cache.HandleCacheUpdateAsync();

            Assert.IsTrue(_cache.IsEmpty());

            //Updates from the server
            await _cache.HandleCacheUpdateAsync();

            Assert.IsTrue(_cache.IsEmpty());
        }

        [Test]
        public async Task
            UpdateCacheFromChangesOnServerAsync_should_not_update_cache_for_same_last_updated_timestamp_from_server()
        {
            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");
            _cache.UpdateCacheFrom(RulesetsForClientTestSupport.SingleRulesetMultipleLanguages());
            _cache.CodigaConfig = new CodigaCodeAnalysisConfig
            {
                Rulesets = new List<string> { "single-ruleset-single-language" }
            };
            _cache.RulesetslastUpdatedTimeStamp = 101L;
            _cache.ConfigFileLastWriteTime = File.GetLastWriteTime(_codigaConfigFile);

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.That(rules, Has.Count.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(101L));

            await _cache.HandleCacheUpdateAsync();

            var updatedRules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.That(updatedRules, Has.Count.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(101L));
        }

        [Test]
        public async Task UpdateCacheFromChangesOnServerAsync_should_update_cache()
        {
            InitConfigAndCache(@"
rulesets:
  - single-ruleset-single-language");
            _cache.UpdateCacheFrom(RulesetsForClientTestSupport.SingleRulesetMultipleLanguages());
            _cache.CodigaConfig = new CodigaCodeAnalysisConfig
            {
                Rulesets = new List<string> { "single-ruleset-single-language" }
            };
            _cache.RulesetslastUpdatedTimeStamp = 102L;
            _cache.ConfigFileLastWriteTime = File.GetLastWriteTime(_codigaConfigFile);

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.That(rules, Has.Count.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(102L));

            await _cache.HandleCacheUpdateAsync();

            var updatedRules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.That(updatedRules, Has.Count.EqualTo(3));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(101L));
        }

        #endregion

        #region Stores multiple rules for multiple rulesets for mulitple languages

        [Test]
        public async Task should_store_rules_from_multiple_rulesets_for_multiple_languages_grouped_by_language()
        {
            InitConfigAndCache(@"
rulesets:
  - multi-rulesets-multi-languages");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Python, _pythonFile, _solutionDirPath);
            Assert.That(rules, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(rules[0].Id, Is.EqualTo($"mixed-ruleset/{RulesetsForClientTestSupport.PythonRule4.Name}"));
                Assert.That(rules[1].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule5.Name}"));
            });

            var updatedRules = await _cache.GetRosieRules(LanguageEnumeration.Java,
                $"{_solutionDirPath}JavaFile.java", _solutionDirPath);
            Assert.That(updatedRules, Has.Count.EqualTo(1));
            Assert.That(updatedRules[0].Id,
                Is.EqualTo($"mixed-ruleset/{RulesetsForClientTestSupport.JavaRule1.Name}"));
        }

        #endregion

        #region GetRosieRules

        [Test]
        public async Task GetRosieRules_should_return_rules_for_empty_ignore_config()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                3,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_2",
                "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task GetRosieRules_should_not_filter_rules_for_ignore_config_with_no_ruleset()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  ");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                3,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_2",
                "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task GetRosieRules_should_not_filter_rules_for_ignore_config_with_no_rule()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                3,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_2",
                "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task GetRosieRules_should_filter_rules_for_ignore_config_with_no_prefix()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                2,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task
            GetRosieRules_should_filter_rule_for_ignore_config_with_one_matching_prefix_with_leading_slash()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix: /javascript");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                2,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task
            GetRosieRules_should_filter_rules_with_ignore_config_with_one_matching_prefix_without_leading_slash()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix: javascript");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                2,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task GetRosieRules_should_filter_rules_with_ignore_config_with_one_matching_file_path_prefix()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix: /javascript_file.js");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                2,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task GetRosieRules_should_filter_rules_with_ignore_config_with_one_matching_directory_path_prefix()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix: /directory");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript,
                $"{_solutionDirPath}directory/javascript_file.js", _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                2,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task GetRosieRules_should_not_filter_rules_with_ignore_config_with_one_prefix_not_matching()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix: not-matching");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                3,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_2", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task
            GetRosieRules_should_not_filter_rules_with_ignore_config_with_one_prefix_containing_double_dots()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix: javascript_file..js");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                3,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_2", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task
            GetRosieRules_should_not_filter_rules_with_ignore_config_with_one_prefix_containing_single_dot_as_folder()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix: directory/./javascript_file.js");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript,
                $"{_solutionDirPath}directory/sub/javascript_file.js", _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                3,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_2", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task
            GetRosieRules_should_not_filter_rules_with_ignore_config_with_one_prefix_containing_double_dots_as_folder()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix: directory/../javascript_file.js");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript,
                $"{_solutionDirPath}directory/sub/javascript_file.js", _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                3,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_2", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task GetRosieRules_should_filter_rules_with_ignore_config_with_one_matching_prefix_of_multiple()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix:
        - not/matching
        - javascript_file.js");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                2,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task GetRosieRules_should_filter_rules_with_ignore_config_with_multiple_matching_prefixes()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix:
        - /javascript
        - javascript_file.js");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                2,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task GetRosieRules_should_not_filter_rules_with_ignore_config_with_multiple_prefixes_not_matching()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix:
        - not-matching
        - also/not/matching");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                3,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_2", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task
            GetRosieRules_should_filter_rules_with_ignore_config_with_multiple_rule_ignore_configurations()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - javascript_rule_2:
      - prefix: javascript_file..js
    - javascript_rule_3:
      - prefix:
        - /javascript_fi");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                2,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_2");
        }

        [Test]
        public async Task GetRosieRules_should_not_filter_rules_when_rule_doesnt_belong_to_ruleset()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - javascript-ruleset:
    - non_javascript_rule:
      - prefix: javascript_file..js");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                3,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_2", "javascript-ruleset/javascript_rule_3");
        }

        [Test]
        public async Task
            GetRosieRules_should_not_filter_rules_when_ruleset_ignore_is_not_present_in_rulesets_property()
        {
            InitConfigAndCache(@"
rulesets:
  - javascript-ruleset
ignore:
  - not-configured-ruleset:
    - javascript_rule_2:
      - prefix: javascript_file.js");

            await _cache.HandleCacheUpdateAsync();

            var rules = await _cache.GetRosieRules(LanguageEnumeration.Javascript, _javaScriptFile, _solutionDirPath);

            ValidateRuleCountAndRuleIds(rules,
                3,
                "javascript-ruleset/javascript_rule_1", "javascript-ruleset/javascript_rule_2", "javascript-ruleset/javascript_rule_3");
        }

        private void ValidateRuleCountAndRuleIds(IReadOnlyList<RosieRule> rules, int count,
            params string[] expectedRuleIds)
        {
            Assert.That(rules, Has.Count.EqualTo(count));

            var actualRuleIds = rules.Select(rule => rule.Id).ToList();
            CollectionAssert.AreEqual(actualRuleIds, expectedRuleIds);
        }

        #endregion

        #region Helpers

        private void InitConfigAndCache(string rawConfig)
        {
            InitCodigaConfig(rawConfig);
            var serviceProvider = ServiceProviderMockSupport.MockServiceProvider(_solutionDirPath);
            _clientProvider = new TestCodigaClientProvider();
            RosieRulesCache.Initialize(serviceProvider, _clientProvider);
            _cache = RosieRulesCache.Instance;
        }

        /// <summary>
        /// Creates the Codiga config file with the provided content.
        /// </summary>
        private void InitCodigaConfig(string rawConfig)
        {
            using var fs = File.Create(_codigaConfigFile);
            var info = Encoding.UTF8.GetBytes(rawConfig);
            fs.Write(info, 0, info.Length);
        }

        /// <summary>
        /// Deletes and recreates (essentially an update) the Codiga config file with the provided content.
        /// </summary>
        private async Task UpdateCodigaConfig(string rawConfig)
        {
            File.Delete(_codigaConfigFile);

            //This delay ensures that the last write time of the new config file is different than the
            // one in the rules cache. In the CI pipeline the test execution tends to be so fast
            // that it is executed in the same millisecond, resulting in the exact same last write time. 
            var task = Task.Delay(TimeSpan.FromMilliseconds(100));
            try
            {
                await task;
            }
            catch (TaskCanceledException)
            {
                return;
            }

            InitCodigaConfig(rawConfig);
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            File.Delete(_codigaConfigFile);
            RosieRulesCache.Dispose();
            _solution = null;
            _solutionDirPath = null;
            _codigaConfigFile = null;
            _cache = null;
        }

        /// <summary>
        /// A client provider whose <c>TryGetClient()</c> methods returns false to signal that there is no
        /// client available.
        /// </summary>
        private class NoCodigaClientProvider : ICodigaClientProvider
        {
            public bool TryGetClient(out ICodigaClient client)
            {
                client = GetClient();
                return false;
            }

            public ICodigaClient GetClient()
            {
                return new TestCodigaClient();
            }
        }
    }
}