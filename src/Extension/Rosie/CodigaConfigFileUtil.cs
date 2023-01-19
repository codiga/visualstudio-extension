using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Extension.Helpers;
using Extension.Rosie.Model.Codiga;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Shell;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Extension.Rosie
{
    /// <summary>
    /// Utility for retrieving information about and from the codiga.yml config file in the Solution.
    /// </summary>
    public static class CodigaConfigFileUtil
    {
        /// <summary>
        /// The naming convention makes it possible to parse the rulesets property as lowercase 'rulesets',
        /// and not as 'Rulesets' (what the C# class property is called in <see cref="CodigaCodeAnalysisConfig"/>.  
        /// </summary>
        private static readonly IDeserializer ConfigDeserializer = new DeserializerBuilder()
            .WithNamingConvention(LowerCaseNamingConvention.Instance)
            .Build();

        /// <summary>
        /// Combines the following validations for the ruleset name:
        /// - it must be at least 5 characters long
        /// - it must be at most 32 character long
        /// - it must start with a lowercase letter or number, but not a dash
        /// - it must consist of lowercase alphanumerical characters and dash
        /// </summary>
        /// <seealso cref="https://regexr.com/730qs"/>
        private static readonly Regex CodigaRulesetNamePattern = new Regex("^[a-z0-9][a-z0-9-]{4,31}$");

        private const string CodigaConfigFileName = "codiga.yml";
        private const string RULESETS = "rulesets";
        private const string IGNORE = "ignore";

        /// <summary>
        /// Looks up the Codiga config file in the provided Solution's root directory,
        /// or in the currently open folder.
        /// <br/>
        /// See https://stackoverflow.com/questions/49278306/how-do-i-find-the-open-folder-in-a-vsix-extension
        /// </summary>
        /// <param name="serviceProvider">The service provider to retrieve information about the solution from.</param>
        /// <returns>The path of the config file, or null if it doesn't exist.</returns>
        public static string? FindCodigaConfigFile(SVsServiceProvider serviceProvider)
        {
            var solutionRoot = SolutionHelper.GetSolutionDir(serviceProvider);
            if (solutionRoot == null)
                return null;

            var codigaConfigFile = Directory
                .EnumerateFiles(solutionRoot, CodigaConfigFileName, SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            return codigaConfigFile != null && File.Exists(codigaConfigFile) ? codigaConfigFile : null;
        }

        /// <summary>
        /// Creates the Codiga config file in the solution's root directory with default Python rulesets.
        /// </summary>
        /// <param name="serviceProvider">The service provider to retrieve information about the solution from.</param>
        public static void CreateCodigaConfigFile(LanguageUtils.LanguageEnumeration language, SVsServiceProvider serviceProvider)
        {
            var solutionRoot = SolutionHelper.GetSolutionDir(serviceProvider);
            if (solutionRoot != null)
            {
                var rulesetConfig = language switch
                {
                    LanguageUtils.LanguageEnumeration.Python => CodigaRulesetConfigs.DefaultPythonRulesetConfig,
                    LanguageUtils.LanguageEnumeration.Javascript => CodigaRulesetConfigs.DefaultJavascriptRulesetConfig,
                    LanguageUtils.LanguageEnumeration.Typescript => CodigaRulesetConfigs.DefaultJavascriptRulesetConfig,
                };
                File.WriteAllText($"{solutionRoot}\\codiga.yml", rulesetConfig);
            }
        }

        /// <summary>
        /// Deserializes the provided raw YAML string (the content of the Codiga config file) to a <see cref="CodigaCodeAnalysisConfig"/>
        /// object, so that ruleset names, ignore prefixes, etc. can be accessed later.
        /// <br/>
        /// It deserializes it using dynamic typing.
        /// </summary>
        /// <param name="rawYamlConfig">The content of the Codiga config file</param>
        /// <returns>The deserialized config file, or <see cref="CodigaCodeAnalysisConfig.EMPTY"/> in case deserialization failed.</returns>
        public static CodigaCodeAnalysisConfig DeserializeConfig(string rawYamlConfig)
        {
            if (string.IsNullOrWhiteSpace(rawYamlConfig))
                return CodigaCodeAnalysisConfig.EMPTY;

            try
            {
                var codigaConfig = new CodigaCodeAnalysisConfig();
                var semiRawConfig = ConfigDeserializer.Deserialize<dynamic>(rawYamlConfig);
                if (semiRawConfig is Dictionary<object, object> properties)
                {
                    SetRulesets(properties, codigaConfig);
                    SetIgnore(properties, codigaConfig);
                }

                return codigaConfig;
            }
            catch
            {
                return CodigaCodeAnalysisConfig.EMPTY;
            }
        }

        /// <summary>
        /// Configures the rulesets in the argument <see cref="CodigaCodeAnalysisConfig"/> based on the deserialized config data.
        /// </summary>
        /// <param name="semiRawConfig">The codiga.yml file as a dictionary of string-object entries</param>
        /// <param name="codigaConfig">The Codiga config in which rulesets are being configured</param>
        private static void SetRulesets(Dictionary<object, object> semiRawConfig, CodigaCodeAnalysisConfig codigaConfig)
        {
            if (semiRawConfig.ContainsKey(RULESETS) && semiRawConfig[RULESETS] is List<object> rulesetNames)
            {
                codigaConfig.Rulesets = rulesetNames
                    .OfType<string>()
                    //Filter out non-string value, and null and empty ruleset names
                    .Where(name => !string.IsNullOrEmpty(name))
                    //Filter out invalid ruleset names
                    .Where(IsRulesetNameValid)
                    .ToList();
            }
        }

        /// <summary>
        /// Configures the ignores in the argument <see cref="CodigaCodeAnalysisConfig"/> based on the deserialized config data.
        /// </summary>
        /// <param name="semiRawConfig">The codiga.yml file as a dictionary of string-object entries</param>
        /// <param name="codigaConfig">The Codiga config in which ignore config is being configured</param>
        private static void SetIgnore(Dictionary<object, object> semiRawConfig, CodigaCodeAnalysisConfig codigaConfig)
        {
            //List of [ruleset name -> rule ignore config] mappings
            if (semiRawConfig.ContainsKey(IGNORE) && semiRawConfig[IGNORE] is List<object> rulesetIgnoreConfigs)
            {
                foreach (var rulesetIgnoreConfig in rulesetIgnoreConfigs)
                {
                    //[ruleset name -> rule ignore config] mappings
                    if (rulesetIgnoreConfig is Dictionary<object, object> rulesetIgnoreDict)
                    {
                        var rulesetIgnore = new RulesetIgnore(
                            rulesetIgnoreDict.Keys.FirstOrDefault() as string,
                            rulesetIgnoreDict.Values.FirstOrDefault());

                        codigaConfig.Ignore.Add(rulesetIgnore.RulesetName, rulesetIgnore);
                    }
                }
            }
        }

        public static bool IsRulesetNameValid(string rulesetName)
        {
            return CodigaRulesetNamePattern.IsMatch(rulesetName);
        }
    }
}