using System.IO;
using Extension.Rosie;
using Extension.Rosie.Model.Codiga;
using Extension.SnippetFormats;
using NUnit.Framework;
using static Tests.ServiceProviderMockSupport;

namespace Tests.Rosie
{
    /// <summary>
    /// Unit test for <see cref="CodigaConfigFileUtil"/>.
    /// </summary>
    [TestFixture]
    internal class CodigaConfigFileUtilTest
    {
        private string _codigaConfigFile;

        #region DeserializeConfig

        [Test]
        public void DeserializeConfig_should_return_empty_config_for_null_raw_config()
        {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(null);

            Assert.That(codigaConfigFile, Is.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        }

        [Test]
        public void DeserializeConfig_should_return_empty_config_for_empty_raw_config()
        {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig("");

            Assert.That(codigaConfigFile, Is.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        }

        [Test]
        public void DeserializeConfig_should_return_empty_config_for_whitespace_only_raw_config()
        {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig("   ");

            Assert.That(codigaConfigFile, Is.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        }
        
        [Test]
        public void DeserializeConfig_should_return_empty_config_for_malformed_rulesets_list()
        {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
- my-csharp-ruleset
- rules:
  - some-rule
- my-other-ruleset");

            Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
            var rulesetNames = codigaConfigFile.Rulesets;

            Assert.That(rulesetNames, Has.Count.EqualTo(2));
            Assert.That(rulesetNames, Contains.Item("my-csharp-ruleset"));
            Assert.That(rulesetNames, Contains.Item("my-other-ruleset"));
        }
        
        [Test]
        public void DeserializeConfig_should_return_non_empty_ruleset_names()
        {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-csharp-ruleset
  - my-other-ruleset
  - an_Inv@lid-name
");

            var rulesetNames = codigaConfigFile.Rulesets;

            Assert.That(rulesetNames, Has.Count.EqualTo(2));
            Assert.That(rulesetNames, Contains.Item("my-csharp-ruleset"));
            Assert.That(rulesetNames, Contains.Item("my-other-ruleset"));
        }

        [Test]
        public void DeserializeConfig_should_return_no_ruleset_name_for_empty_rulesets_list()
        {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - 
");

            var rulesetNames = codigaConfigFile.Rulesets;

            Assert.That(rulesetNames, Is.Empty);
        }

        [Test]
        public void DeserializeConfig_should_return_filtered_ruleset_names_when_there_is_empty_list_item()
        {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-csharp-ruleset
  - my-other-ruleset
  - 
  - some-ruleset
");

            var rulesetNames = codigaConfigFile.Rulesets;

            Assert.That(rulesetNames, Has.Count.EqualTo(3));
            Assert.That(rulesetNames, Contains.Item("my-csharp-ruleset"));
            Assert.That(rulesetNames, Contains.Item("my-other-ruleset"));
            Assert.That(rulesetNames, Contains.Item("some-ruleset"));
        }

        [Test]
        public void DeserializeConfig_should_return_filtered_ruleset_names_when_there_is_non_plain_text_list_item()
        {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-csharp-ruleset
  - rules:
    - some-rule
  - my-other-ruleset
");

            var rulesetNames = codigaConfigFile.Rulesets;

            Assert.That(rulesetNames, Has.Count.EqualTo(2));
            Assert.That(rulesetNames, Contains.Item("my-csharp-ruleset"));
            Assert.That(rulesetNames, Contains.Item("my-other-ruleset"));
        }

        [Test]
        public void DeserializeConfig_should_return_no_ruleset_name_for_missing_ruleset_property()
        {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
not-rulesets:
  - my-csharp-ruleset
  - my-other-ruleset");

            var rulesetNames = codigaConfigFile.Rulesets;

            Assert.That(rulesetNames, Is.Empty);
        }

        [Test]
        public void DeserializeConfig_should_return_no_ruleset_name_for_non_sequence_empty_rulesets_list()
        {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  ");

            var rulesetNames = codigaConfigFile.Rulesets;

            Assert.That(rulesetNames, Is.Empty);
        }

        [Test]
        public void DeserializeConfig_should_return_no_ruleset_name_for_non_sequence_rulesets_property()
        {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  rules:");

            var rulesetNames = codigaConfigFile.Rulesets;

            Assert.That(rulesetNames, Is.Empty);
        }

        [Test]
        public void DeserializeConfig_should_return_empty_ignore_config_for_non_property_ignore() {
            var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore");

            Assert.That(codigaConfigFile, Is.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        }
    
        [Test]
    public void DeserializeConfig_should_return_ignore_config_for_no_ignore_item() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:");
        
        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Is.Empty);
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_blank_ignore_item() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - ");
    
        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Is.Empty);
    }
    
    [Test]
    public void DeserializeConfig_should_return_empty_ignore_config_for_string_ignore_item() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset");

        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Is.Empty);
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_empty_ruleset_name_ignore_property() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:");

        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Is.Empty);
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_string_rule_name_ignore_property() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1");
    
        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Is.Empty);
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_empty_rule_name_ignore_property() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1:");
        
        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Is.Empty);
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_string_prefix_ignore_property() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1:
      - prefix");
        
        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Is.Empty);
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_empty_prefix_ignore_property() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1:
      - prefix:");
        
        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Is.Empty);
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_blank_prefix() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1:
      - prefix:     ");
        
        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Is.Empty);
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_single_prefix() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1:
      - prefix: /path/to/file/to/ignore");

        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[0], Is.EqualTo("/path/to/file/to/ignore"));
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_single_prefix_as_list() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1:
      - prefix:
        - /path/to/file/to/ignore");

        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[0], Is.EqualTo("/path/to/file/to/ignore"));
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_multiple_prefixes_as_list() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1:
      - prefix:
        - /path1
        - /path2");
    
        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Has.Count.EqualTo(2));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[0], Is.EqualTo("/path1"));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[1], Is.EqualTo("/path2"));
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_multiple_rule_ignores() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1:
      - prefix:
        - /path1
        - /path2
    - rule2");
        
        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(2));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Has.Count.EqualTo(2));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[0], Is.EqualTo("/path1"));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[1], Is.EqualTo("/path2"));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule2"].Prefixes, Is.Empty);
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_without_rulesets() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
ignore:
  - my-python-ruleset:
    - rule1:
      - prefix:
        - /path1
        - /path2");

        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Has.Count.EqualTo(2));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[0], Is.EqualTo("/path1"));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[1], Is.EqualTo("/path2"));
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_duplicate_prefix_properties() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1:
      - prefix:
        - /path1
        - /path2
      - prefix: /path3");
        
        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[0], Is.EqualTo("/path3"));
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_duplicate_prefix_values() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1:
      - prefix:
        - /path1
        - /path1");

        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes, Has.Count.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[0], Is.EqualTo("/path1"));
    }
    
    [Test]
    public void DeserializeConfig_should_return_ignore_config_for_multiple_ruleset_ignores() {
        var codigaConfigFile = CodigaConfigFileUtil.DeserializeConfig(@"
rulesets:
  - my-python-ruleset
  - my-other-ruleset
ignore:
  - my-python-ruleset:
    - rule1:
      - prefix:
        - /path1
        - /path2
    - rule2
  - my-other-ruleset:
    - rule3:
      - prefix: /another/path");

        Assert.That(codigaConfigFile, Is.Not.EqualTo(CodigaCodeAnalysisConfig.EMPTY));
        Assert.That(codigaConfigFile.Ignore.Count, Is.EqualTo(2));
        
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores.Count, Is.EqualTo(2));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes.Count, Is.EqualTo(2));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[0], Is.EqualTo("/path1"));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule1"].Prefixes[1], Is.EqualTo("/path2"));
        Assert.That(codigaConfigFile.Ignore["my-python-ruleset"].RuleIgnores["rule2"].Prefixes, Is.Empty);
        
        Assert.That(codigaConfigFile.Ignore["my-other-ruleset"].RuleIgnores.Count, Is.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-other-ruleset"].RuleIgnores["rule3"].Prefixes.Count, Is.EqualTo(1));
        Assert.That(codigaConfigFile.Ignore["my-other-ruleset"].RuleIgnores["rule3"].Prefixes[0], Is.EqualTo("/another/path"));
    }
    
    #endregion

    #region FindCodigaConfigFile

        [Test]
        public void FindCodigaConfigFile_should_return_null_for_missing_solution_root()
        {
            var serviceProvider = MockServiceProvider("/");

            var configFile = CodigaConfigFileUtil.FindCodigaConfigFile(serviceProvider);

            Assert.That(configFile, Is.Null);
        }

        [Test]
        public void FindCodigaConfigFile_should_return_null_for_missing_codiga_config_file()
        {
            var serviceProvider = MockServiceProvider(Path.GetTempPath());

            var configFile = CodigaConfigFileUtil.FindCodigaConfigFile(serviceProvider);

            Assert.That(configFile, Is.Null);
        }

        [Test]
        public void FindCodigaConfigFile_should_return_path_of_codiga_config_file()
        {
            var serviceProvider = MockServiceProvider(Path.GetTempPath());
            _codigaConfigFile = $"{Path.GetTempPath()}codiga.yml";

            using var fs = File.Create(_codigaConfigFile);

            var configFile = CodigaConfigFileUtil.FindCodigaConfigFile(serviceProvider);

            Assert.That(configFile, Is.EqualTo(_codigaConfigFile));
        }

        #endregion

        #region CreateCodigaConfigFile

        [Test]
        public void CreateCodigaConfigFile_should_create_codiga_yml_with_python_rulesets()
        {
            var serviceProvider = MockServiceProvider(Path.GetTempPath());
            _codigaConfigFile = $"{Path.GetTempPath()}codiga.yml";

            CodigaConfigFileUtil.CreateCodigaConfigFile(LanguageUtils.LanguageEnumeration.Python, serviceProvider);

            Assert.That(File.Exists(_codigaConfigFile), Is.True);
            Assert.That(File.ReadAllText(_codigaConfigFile), Is.EqualTo("rulesets:\n" +
                                                                        "  - python-security\n" +
                                                                        "  - python-best-practices\n" +
                                                                        "  - python-code-style"));
        }

        [Test]
        public void CreateCodigaConfigFile_should_create_codiga_yml_with_js_rulesets_for_js()
        {
            var serviceProvider = MockServiceProvider(Path.GetTempPath());
            _codigaConfigFile = $"{Path.GetTempPath()}codiga.yml";

            CodigaConfigFileUtil.CreateCodigaConfigFile(LanguageUtils.LanguageEnumeration.Javascript, serviceProvider);

            Assert.That(File.Exists(_codigaConfigFile), Is.True);
            Assert.That(File.ReadAllText(_codigaConfigFile), Is.EqualTo("rulesets:\n" +
                                                                        "  - jsx-a11y\n" +
                                                                        "  - jsx-react\n" +
                                                                        "  - react-best-practices"));
        }

        [Test]
        public void CreateCodigaConfigFile_should_create_codiga_yml_with_js_rulesets_for_ts()
        {
            var serviceProvider = MockServiceProvider(Path.GetTempPath());
            _codigaConfigFile = $"{Path.GetTempPath()}codiga.yml";

            CodigaConfigFileUtil.CreateCodigaConfigFile(LanguageUtils.LanguageEnumeration.Typescript, serviceProvider);

            Assert.That(File.Exists(_codigaConfigFile), Is.True);
            Assert.That(File.ReadAllText(_codigaConfigFile), Is.EqualTo("rulesets:\n" +
                                                                        "  - jsx-a11y\n" +
                                                                        "  - jsx-react\n" +
                                                                        "  - react-best-practices"));
        }

        #endregion

        #region Invalid ruleset names

        [Test]
        //Invalid content
        [TestCase("   ", ExpectedResult = false)]
        [TestCase("\n", ExpectedResult = false)]
        [TestCase("-", ExpectedResult = false)]
        [TestCase("- ", ExpectedResult = false)]
        [TestCase("-ruleset-name", ExpectedResult = false)]
        [TestCase("ruleset_name", ExpectedResult = false)]
        [TestCase("AwEsom3Stuff!", ExpectedResult = false)]
        [TestCase("ruleset%67", ExpectedResult = false)]
        [TestCase("ruleset name", ExpectedResult = false)]
        [TestCase("ruleset-with-german-ß-in-it", ExpectedResult = false)]
        //Invalid length
        [TestCase("", ExpectedResult = false)]
        [TestCase("r", ExpectedResult = false)]
        [TestCase("ru", ExpectedResult = false)]
        [TestCase("rul", ExpectedResult = false)]
        [TestCase("rule", ExpectedResult = false)]
        [TestCase("rule-that-longer-than-thirty-two-", ExpectedResult = false)]
        //Valid cases
        [TestCase("5long", ExpectedResult = true)]
        [TestCase("123456789", ExpectedResult = true)]
        [TestCase("csharp-ruleset-63", ExpectedResult = true)]
        [TestCase("csharp-ruleset-name", ExpectedResult = true)]
        public bool IsRulesetNameValid_should_return_ruleset_name_validity(string rulesetName)
        {
            return CodigaConfigFileUtil.IsRulesetNameValid(rulesetName);
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_codigaConfigFile))
                File.Delete(_codigaConfigFile);
        }
    }
}