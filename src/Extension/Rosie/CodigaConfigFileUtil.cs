using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Solution = EnvDTE.Solution;

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
        private static readonly Regex CodigaRulesetNamePattern = new Regex("^[a-z0-9][a-z0-9-]{4,}$");
        private const string CodigaConfigFileName = "codiga.yml";

        /// <summary>
        /// Looks up the Codiga config file in the provided Solution's root directory.
        /// </summary>
        /// <param name="solution">The current solution to find the config file in</param>
        /// <returns>The path of the config file, or null if it doesn't exist.</returns>
        public static string? FindCodigaConfigFile(Solution solution)
        {
            var solutionRoot = Path.GetDirectoryName(solution.FullName);
            if (solutionRoot == null)
                return null;

            var codigaConfigFile = Directory
                .EnumerateFiles(solutionRoot, CodigaConfigFileName, SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            return codigaConfigFile != null && File.Exists(codigaConfigFile) ? codigaConfigFile : null;
        }

        /// <summary>
        /// Collects the list of valid ruleset names from the provided configuration object.
        /// </summary>
        /// <param name="config">The configuration containing the rulesets.</param>
        /// <returns>The list of ruleset names or empty list if there is no config or no ruleset.</returns>
        public static List<string> CollectRulesetNames(CodigaCodeAnalysisConfig? config)
        {
            return config?.Rulesets == null
                ? new List<string>()
                : config.Rulesets
                    //Filter out non-string value, and null and empty ruleset names
                    .Where(name => !string.IsNullOrEmpty(name))
                    //Filter out invalid ruleset names
                    .Where(IsRulesetNameValid)
                    .ToList();
        }

        /// <summary>
        /// Deserializes the provided raw YAML string (the content of the Codiga config file) to a <see cref="CodigaCodeAnalysisConfig"/>
        /// object, so that ruleset names can be accessed later.
        ///
        /// First, it tries to deserialize directly into a <c>CodigaCodeAnalysisConfig</c> instance, and if that fails,
        /// it tries to deserialize it using dynamic typing. This is necessary because when the config file is configured e.g. like this:
        /// <code>
        /// rulesets:
        /// - my-csharp-ruleset
        /// - rules:
        ///     - some-rule
        /// - my-other-ruleset
        /// </code>
        /// it would fail with an exception on the <c>rules</c> property, and would not return the rest of the ruleset names.
        /// </summary>
        /// <param name="rawYamlConfig">The content of the Codiga config file</param>
        /// <returns>The deserialized config file, or null in case deserialization couldn't happen.</returns>
        public static CodigaCodeAnalysisConfig? DeserializeConfig(string rawYamlConfig)
        {
            if (string.IsNullOrWhiteSpace(rawYamlConfig))
                return null;
            
            try
            {
                return ConfigDeserializer.Deserialize<CodigaCodeAnalysisConfig>(rawYamlConfig);
            }
            catch
            {
                var semiRawConfigFile = ConfigDeserializer.Deserialize<dynamic>(rawYamlConfig);
                if (semiRawConfigFile is Dictionary<object, object> properties
                    //If there is one property, and it is called 'rulesets'
                    && properties.Keys.Count == 1 && properties.ContainsKey("rulesets")
                    //If the value of 'rulesets' is a non-empty list
                    && properties["rulesets"] is List<object> rulesetNames && rulesetNames.Count > 0)
                {
                    return new CodigaCodeAnalysisConfig
                    {
                        Rulesets = rulesetNames.OfType<string>().ToList()
                    };
                }

                return null;
            }
        }

        public static bool IsRulesetNameValid(string rulesetName)
        {
            return CodigaRulesetNamePattern.IsMatch(rulesetName);
        }
    }
}
