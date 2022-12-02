using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE;
using Extension.Caching;
using Extension.Rosie;
using Extension.SnippetFormats;
using GraphQLClient;
using Moq;
using NUnit.Framework;

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
        private Mock<Solution> _solution;
        private string _solutionDirPath;
        private string _codigaConfigFile;
        private ICodigaClientProvider _clientProvider;
        private RosieRulesCache _cache;

        /// <summary>
        /// Initializes the test with a Solution directory with a mock Solution,
        /// and a codiga.yml in that directory,
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _solutionDirPath = Path.GetTempPath();
            _codigaConfigFile = $"{_solutionDirPath}codiga.yml";

            _solution = new Mock<Solution>();
            //Necessary to mock for CodigaConfigFileUtil.FindCodigaConfigFile()
            _solution.Setup(s => s.FullName).Returns(_solutionDirPath);
        }

        #region HandleCacheUpdate

        [Test]
        public void HandleCacheUpdate_should_return_no_codiga_client_for_missing_codiga_client()
        {
            RosieRulesCache.Initialize(_solution.Object, new NoCodigaClientProvider());
            var updateResult = RosieRulesCache.Instance.HandleCacheUpdate();

            Assert.AreEqual(updateResult, RosieRulesCache.UpdateResult.NoCodigaClient);
        }

        [Test]
        public void HandleCacheUpdate_should_clear_cache_and_return_no_config_file_for_missing_config_file()
        {
            //Configure a Codiga config file, and initialize the cache with some rules

            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");

            Assert.That(RosieRulesCache.IsInitializedWithRules, Is.False);

            _cache.HandleCacheUpdate();

            //Delete the Codiga config file and try to update the cache

            File.Delete(_codigaConfigFile);

            var updateResult = _cache.HandleCacheUpdate();

            Assert.AreEqual(updateResult, RosieRulesCache.UpdateResult.NoConfigFile);
            Assert.That(_cache.RulesetNames, Is.Empty);
            Assert.That(_cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python),
                Is.Empty);
            Assert.That(RosieRulesCache.IsInitializedWithRules, Is.True);
        }

        //Handles the UpdateCacheFromModifiedCodigaConfigFile branch
        [Test]
        public void HandleCacheUpdate_should_populate_empty_cache_from_codiga_config_file()
        {
            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");

            var updateResult = _cache.HandleCacheUpdate();

            Assert.AreEqual(updateResult, RosieRulesCache.UpdateResult.Success);

            var rules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rules[0].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
            Assert.That(rules[1].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
            Assert.That(rules[2].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule3.Name}"));
        }

        //Handles the UpdateCacheFromModifiedCodigaConfigFile branch
        [Test]
        public void HandleCacheUpdate_should_update_non_empty_cache_from_codiga_config_file()
        {
            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");

            _cache.HandleCacheUpdate();

            var rules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rules[0].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
            Assert.That(rules[1].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
            Assert.That(rules[2].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule3.Name}"));

            UpdateCodigaConfig(@"
rulesets:
  - multipleRulesetsSingleLanguage");

            var updateResult = _cache.HandleCacheUpdate();

            Assert.AreEqual(updateResult, RosieRulesCache.UpdateResult.Success);

            var rulesMulti = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rulesMulti[0].Id,
                Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
            Assert.That(rulesMulti[1].Id,
                Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
            Assert.That(rulesMulti[2].Id,
                Is.EqualTo($"python-ruleset-2/{RulesetsForClientTestSupport.PythonRule3.Name}"));
        }

        //Handles the UpdateCacheFromChangesOnServer branch
        [Test]
        public void HandleCacheUpdate_should_update_non_empty_cache_from_server()
        {
            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");

            _cache.UpdateCacheFrom(RulesetsForClientTestSupport.SingleRulesetMultipleLanguages());
            _cache.RulesetslastUpdatedTimeStamp = 102L;
            _cache.ConfigFileLastWriteTime = File.GetLastWriteTime(_codigaConfigFile);
            _cache.RulesetNames = new List<string> { "singleRulesetSingleLanguage" };

            var rules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rules.Count, Is.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(102L));

            _cache.HandleCacheUpdate();

            var updatedRules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(updatedRules.Count, Is.EqualTo(3));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(101L));
        }

        #endregion

        #region UpdateCacheFromModifiedCodigaConfigFile

        [Test]
        public void UpdateCacheFromModifiedCodigaConfigFile_should_clear_cache_for_null_deserialization_result()
        {
            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");

            _cache.HandleCacheUpdate();

            Assert.IsFalse(_cache.IsEmpty());

            UpdateCodigaConfig(@"rulesets:");

            _cache.HandleCacheUpdate();

            Assert.IsTrue(_cache.IsEmpty());
        }

        [Test]
        public void UpdateCacheFromModifiedCodigaConfigFile_should_clear_cache_for_no_rulesets_in_config_file()
        {
            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");
            
            _cache.HandleCacheUpdate();

            Assert.IsFalse(_cache.IsEmpty());

            UpdateCodigaConfig(@"
rulesets:
  - ");

            _cache.HandleCacheUpdate();

            Assert.IsTrue(_cache.IsEmpty());
        }

        [Test]
        public void UpdateCacheFromModifiedCodigaConfigFile_should_not_update_for_null_rulesets_returned_from_server()
        {
            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");
            
            _cache.HandleCacheUpdate();

            var rules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rules[0].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
            Assert.That(rules[1].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
            Assert.That(rules[2].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule3.Name}"));

            UpdateCodigaConfig(@"
rulesets:
  - erroredRuleset");
            
            _cache.HandleCacheUpdate();

            var updatedRules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(updatedRules[0].Id,
                Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
            Assert.That(updatedRules[1].Id,
                Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
            Assert.That(updatedRules[2].Id,
                Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule3.Name}"));
        }

        [Test]
        public void UpdateCacheFromModifiedCodigaConfigFile_should_clear_cache_for_no_rulesets_returned_from_server()
        {
            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");
            
            _cache.HandleCacheUpdate();

            Assert.IsFalse(_cache.IsEmpty());

            UpdateCodigaConfig(@"
rulesets:
  - nonExistentRuleset");

            _cache.HandleCacheUpdate();

            Assert.IsTrue(_cache.IsEmpty());
        }

        [Test]
        public void UpdateCacheFromModifiedCodigaConfigFile_should_update_cache_for_same_last_updated_timestamp()
        {
            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");
            
            _cache.HandleCacheUpdate();
            _cache.RulesetslastUpdatedTimeStamp = 100L;

            UpdateCodigaConfig(@"
rulesets:
  - singleRulesetMultipleLanguagesDefaultTimestamp");

            _cache.HandleCacheUpdate();

            var updatedRules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(updatedRules, Has.Count.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(100L));
        }

        [Test]
        public void UpdateCacheFromModifiedCodigaConfigFile_should_update_cache_for_different_last_updated_timestamp()
        {
            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");
            
            _cache.HandleCacheUpdate();
            _cache.RulesetslastUpdatedTimeStamp = 100L;

            UpdateCodigaConfig(@"
rulesets:
  - singleRulesetMultipleLanguages");

            _cache.HandleCacheUpdate();

            var updatedRules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(updatedRules, Has.Count.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(102L));
        }

        #endregion

        #region UpdateCacheFromChangesOnServer

        [Test]
        public void UpdateCacheFromChangesOnServer_should_not_update_cache_for_no_ruleset_name()
        {
            InitConfigAndCache(@"
rulesets:
  - ");

            //Updates for "change" in the config file
            _cache.HandleCacheUpdate();

            Assert.IsTrue(_cache.IsEmpty());

            //Updates from the server
            _cache.HandleCacheUpdate();

            Assert.IsTrue(_cache.IsEmpty());
        }

        [Test]
        public void UpdateCacheFromChangesOnServer_should_not_update_cache_for_same_last_updated_timestamp_from_server()
        {
            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");
            _cache.UpdateCacheFrom(RulesetsForClientTestSupport.SingleRulesetMultipleLanguages());
            _cache.RulesetNames = new List<string> { "singleRulesetSingleLanguage" };
            _cache.RulesetslastUpdatedTimeStamp = 101L;
            _cache.ConfigFileLastWriteTime = File.GetLastWriteTime(_codigaConfigFile);

            var rules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rules, Has.Count.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(101L));

            _cache.HandleCacheUpdate();

            var updatedRules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(updatedRules, Has.Count.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(101L));
        }

        [Test]
        public void UpdateCacheFromChangesOnServer_should_update_cache()
        {
            InitConfigAndCache(@"
rulesets:
  - singleRulesetSingleLanguage");
            _cache.UpdateCacheFrom(RulesetsForClientTestSupport.SingleRulesetMultipleLanguages());
            _cache.RulesetNames = new List<string> { "singleRulesetSingleLanguage" };
            _cache.RulesetslastUpdatedTimeStamp = 102L;
            _cache.ConfigFileLastWriteTime = File.GetLastWriteTime(_codigaConfigFile);

            var rules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rules, Has.Count.EqualTo(2));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(102L));

            _cache.HandleCacheUpdate();

            var updatedRules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(updatedRules, Has.Count.EqualTo(3));
            Assert.That(_cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(101L));
        }

        #endregion

        #region Stores multiple rules for multiple rulesets for mulitple languages

        [Test]
        public void should_store_rules_from_multiple_rulesets_for_multiple_languages_grouped_by_language()
        {
            InitConfigAndCache(@"
rulesets:
  - multipleRulesetsMultipleLanguages");

            _cache.HandleCacheUpdate();

            var rules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rules, Has.Count.EqualTo(2));
            Assert.That(rules[0].Id, Is.EqualTo($"mixed-ruleset/{RulesetsForClientTestSupport.PythonRule4.Name}"));
            Assert.That(rules[1].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule5.Name}"));

            var updatedRules = _cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Java);
            Assert.That(updatedRules, Has.Count.EqualTo(1));
            Assert.That(updatedRules[0].Id,
                Is.EqualTo($"mixed-ruleset/{RulesetsForClientTestSupport.JavaRule1.Name}"));
        }

        #endregion

        #region Helpers

        private void InitConfigAndCache(string rawConfig)
        {
            InitCodigaConfig(rawConfig);
            _clientProvider = new TestCodigaClientProvider();
            RosieRulesCache.Initialize(_solution.Object, _clientProvider);
            _cache = RosieRulesCache.Instance;
        }

        private void InitCodigaConfig(string rawConfig)
        {
            using var fs = File.Create(_codigaConfigFile);
            var info = Encoding.UTF8.GetBytes(rawConfig);
            fs.Write(info, 0, info.Length);
        }

        private void UpdateCodigaConfig(string rawConfig)
        {
            File.Delete(_codigaConfigFile);
            InitCodigaConfig(rawConfig);
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            File.Delete(_codigaConfigFile);
            RosieRulesCache.Dispose();
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