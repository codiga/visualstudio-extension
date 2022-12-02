using System.Collections.Generic;
using System.IO;
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

        [SetUp]
        public void Setup()
        {
            _solutionDirPath = Path.GetTempPath();
            _codigaConfigFile = $"{_solutionDirPath}codiga.yml";

            _solution = new Mock<Solution>();
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

            InitCodigaConfig(@"
rulesets:
  - singleRulesetSingleLanguage");

            var clientProvider = new TestCodigaClientProvider();
            RosieRulesCache.Initialize(_solution.Object, clientProvider);

            Assert.That(RosieRulesCache.IsInitializedWithRules, Is.False);

            var cache = RosieRulesCache.Instance;
            cache.HandleCacheUpdate();

            //Delete the Codiga config file and try to update the cache

            File.Delete(_codigaConfigFile);

            var updateResult = cache.HandleCacheUpdate();

            Assert.AreEqual(updateResult, RosieRulesCache.UpdateResult.NoConfigFile);
            Assert.That(cache.RulesetNames, Is.Empty);
            Assert.That(cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python),
                Is.Empty);
            Assert.That(RosieRulesCache.IsInitializedWithRules, Is.True);
        }

        [Test]
        public void HandleCacheUpdate_should_populate_empty_cache_from_codiga_config_file()
        {
            InitCodigaConfig(@"
rulesets:
  - singleRulesetSingleLanguage");

            var clientProvider = new TestCodigaClientProvider();
            RosieRulesCache.Initialize(_solution.Object, clientProvider);
            var cache = RosieRulesCache.Instance;

            var updateResult = cache.HandleCacheUpdate();

            Assert.AreEqual(updateResult, RosieRulesCache.UpdateResult.Success);

            var rules = cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rules[0].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
            Assert.That(rules[1].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
            Assert.That(rules[2].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule3.Name}"));
        }

        [Test]
        public void HandleCacheUpdate_should_update_non_empty_cache_from_codiga_config_file()
        {
            InitCodigaConfig(@"
rulesets:
  - singleRulesetSingleLanguage");

            var clientProvider = new TestCodigaClientProvider();
            RosieRulesCache.Initialize(_solution.Object, clientProvider);
            var cache = RosieRulesCache.Instance;

            cache.HandleCacheUpdate();

            var rules = cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rules[0].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
            Assert.That(rules[1].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
            Assert.That(rules[2].Id, Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule3.Name}"));

            UpdateCodigaConfig(@"
rulesets:
  - multipleRulesetsSingleLanguage");

            var updateResult = cache.HandleCacheUpdate();

            Assert.AreEqual(updateResult, RosieRulesCache.UpdateResult.Success);

            var rulesMulti = cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rulesMulti[0].Id,
                Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule1.Name}"));
            Assert.That(rulesMulti[1].Id,
                Is.EqualTo($"python-ruleset/{RulesetsForClientTestSupport.PythonRule2.Name}"));
            Assert.That(rulesMulti[2].Id,
                Is.EqualTo($"python-ruleset-2/{RulesetsForClientTestSupport.PythonRule3.Name}"));
        }

        [Test]
        public void HandleCacheUpdate_should_update_non_empty_cache_from_server()
        {
            InitCodigaConfig(@"
rulesets:
  - singleRulesetSingleLanguage");

            var clientProvider = new TestCodigaClientProvider();
            RosieRulesCache.Initialize(_solution.Object, clientProvider);
            var cache = RosieRulesCache.Instance;

            cache.UpdateCacheFrom(RulesetsForClientTestSupport.SingleRulesetMultipleLanguages());
            cache.RulesetslastUpdatedTimeStamp = 102L;
            cache.ConfigFileLastWriteTime = File.GetLastWriteTime(_codigaConfigFile);
            cache.RulesetNames = new List<string> { "singleRulesetSingleLanguage" };

            var rules = cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(rules.Count, Is.EqualTo(2));
            Assert.That(cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(102L));

            cache.HandleCacheUpdate();

            var updatedRules = cache.GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration.Python);
            Assert.That(updatedRules.Count, Is.EqualTo(3));
            Assert.That(cache.RulesetslastUpdatedTimeStamp, Is.EqualTo(101L));
        }

        #endregion

        #region Helpers

        private void InitCodigaConfig(string rawConfig)
        {
            File.WriteAllText(_codigaConfigFile, rawConfig);
        }

        private void UpdateCodigaConfig(string rawConfig)
        {
            InitCodigaConfig(rawConfig);
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            RosieRulesCache.Dispose();
            File.Delete(_codigaConfigFile);
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