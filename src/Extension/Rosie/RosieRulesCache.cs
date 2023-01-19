using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Extension.Caching;
using Extension.Helpers;
using Extension.Rosie.Annotation;
using Extension.Rosie.Model;
using Extension.Rosie.Model.Codiga;
using GraphQLClient;
using GraphQLClient.Model.Rosie;
using Microsoft.VisualStudio.Shell;
using LanguageEnumeration = Extension.SnippetFormats.LanguageUtils.LanguageEnumeration;

namespace Extension.Rosie
{
    /// <summary>
    /// Caches Rosie rules based on the most up-to-date version of rules and rulesets on the Codiga server.
    /// </summary>
    public class RosieRulesCache
    {
        public const string CacheLastUpdatedTimeStampProp = "CacheLastUpdatedTimeStamp";
        private const int PollIntervalInMillis = 10000;
        private static readonly IReadOnlyList<RosieRule> NoRule = new List<RosieRule>(); 

        private ICodigaClientProvider _clientProvider;
        private CancellationTokenSource _cancellationTokenSource;
        
        /// <summary>
        /// Mapping the rules to their target languages, because this way
        /// <ul>
        ///     <li>retrieving the rules from this cache is much easier,</li>
        ///     <li>filtering the rules by language each time a request has to be sent to
        ///     the Rosie service is not necessary.</li>
        /// </ul>
        /// NOTE: in the future, when the codiga.yml config file will be recognized at locations other than the project root,
        /// the cache key will probably have to be changed.
        /// </summary>
        private IDictionary<LanguageEnumeration, RosieRulesCacheValue> _cachedRules;

        /// <summary>
        /// The timestamp of the last update on the Codiga server for the rulesets cached (and configured in codiga.yml).
        /// </summary>
        internal long RulesetslastUpdatedTimeStamp { get; set; } = -1L;

        /// <summary>
        /// The timestamp when this cache was last updated. It is not the same as <see cref="RulesetslastUpdatedTimeStamp"/>, and we need to
        /// handle them separately, because we want to update tagging in non-active documents only when they get focus,
        /// and they haven't been tagged for the latest changes in this cache.
        /// </summary>
        /// <see cref="RosieViolationTaggerProvider"/>
        public long CacheLastUpdatedTimeStamp { get; set; } = -1L;

        /// <summary>
        /// DateTime.MinValue means the last write time of codiga.yml hasn't been set, or there is no codiga.yml file in the Solution root.
        /// </summary>
        internal DateTime ConfigFileLastWriteTime { get; set; } = DateTime.MinValue;

        public CodigaCodeAnalysisConfig CodigaConfig { get; set; }

        /// <summary>
        /// The cache is considered initialized with rules right after the response is received from <see cref="ICodigaClient.GetRulesetsForClientAsync"/>,
        /// or when there is no <see cref="ICodigaClient"/> to use.
        /// </summary>
        public static bool IsInitializedWithRules;
        
        public static RosieRulesCache? Instance { get; set; }

        /// <summary>
        /// Used to retrieve information about the solution, or the open directory,
        /// in <see cref="CodigaConfigFileUtil"/>.
        /// Null only in case of testing.
        /// </summary>
        private SVsServiceProvider? _serviceProvider;

        private RosieRulesCache() : this(new DefaultCodigaClientProvider())
        {
        }
        
        //For testing
        private RosieRulesCache(ICodigaClientProvider clientProvider)
        {
            _clientProvider = clientProvider;
            _cachedRules = new ConcurrentDictionary<LanguageEnumeration, RosieRulesCacheValue>();
            CodigaConfig = CodigaCodeAnalysisConfig.EMPTY;
        }

        public static void Initialize(bool startPolling = true)
        {
            TextWriterTraceListener tr1 = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(tr1);
            
            Instance = new RosieRulesCache();
            if (startPolling)
                Instance.StartPolling();
        }
        
        //For testing
        public static void Initialize(SVsServiceProvider? serviceProvider, ICodigaClientProvider clientProvider)
        {
            Instance = new RosieRulesCache(clientProvider) { _serviceProvider = serviceProvider };
        }

        #region Polling and update

        /// <summary>
        /// Starts the background thread that checks for ruleset updates both in the Codiga config file
        /// and on the server, and updates the cache when there is any update.
        /// </summary>
        /// <returns></returns>
        private void StartPolling()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            //Retrieve the DTE object from which the Solution can be accessed
            ThreadHelper.ThrowIfNotOnUIThread();
            _serviceProvider = VS.GetMefService<SVsServiceProvider>();

            PollRulesetsAsync(_cancellationTokenSource.Token);
        }

        private async Task PollRulesetsAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                switch (await HandleCacheUpdateAsync())
                {
                    case UpdateResult.NoCodigaClient:
                    {
                        IsInitializedWithRules = true;
                        return;
                    }
                    
                    //If there is no config file, or there is one, and the rule update was successful,
                    //Wait for 'PollIntervalInSeconds' before starting a new round of polling. 
                    case UpdateResult.NoConfigFile:
                    case UpdateResult.Success:
                    default:
                    {
                        //The combination of 'while(true)' and 'Task.Delay()' forms the periodic polling of rulesets.
                        var delay = Task.Delay(TimeSpan.FromMilliseconds(PollIntervalInMillis), cancellationToken);
                        try
                        {
                            await delay;
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }

                        break;
                    }
                }
            }
        }

        public enum UpdateResult
        {
            NoCodigaClient,
            NoConfigFile,
            Success
        }

        public async Task<UpdateResult> HandleCacheUpdateAsync()
        {
            if (!_clientProvider.TryGetClient(out var client))
                return UpdateResult.NoCodigaClient;

            var codigaConfigFile = CodigaConfigFileUtil.FindCodigaConfigFile(_serviceProvider);

            if (codigaConfigFile == null || !File.Exists(codigaConfigFile))
            {
                ClearCache();
                //Since the config file no longer exists, its last write time is reset too
                ConfigFileLastWriteTime = DateTime.MinValue;
                IsInitializedWithRules = true;
                return UpdateResult.NoConfigFile;
            }

            //If the Codiga config file has changed (its last write time doesn't match its previous write time)
            if (ConfigFileLastWriteTime.CompareTo(File.GetLastWriteTime(codigaConfigFile)) != 0)
                await UpdateCacheFromModifiedCodigaConfigFileAsync(codigaConfigFile, client);
            else
                await UpdateCacheFromChangesOnServerAsync(client);

            return UpdateResult.Success;
        }

        /// <summary>
        /// Handles when there was a change in the codiga.yml file.
        /// </summary>
        private async Task UpdateCacheFromModifiedCodigaConfigFileAsync(string codigaConfigFile, ICodigaClient client)
        {
            ConfigFileLastWriteTime = File.GetLastWriteTime(codigaConfigFile);
            var rawCodigaConfig = File.ReadAllText(codigaConfigFile);
            var codigaConfig = CodigaConfigFileUtil.DeserializeConfig(rawCodigaConfig);
            //If the config file is not configured properly, we clear the cache
            if (codigaConfig.Rulesets.Count == 0)
            {
                ClearCache();
                return;
            }
            
            CodigaConfig = codigaConfig;

            //If there is at least on ruleset name, we can make a request with them
            if (CodigaConfig.Rulesets.Count > 0)
            {
                try
                {
                    Debug.WriteLine($"Fetching rulesets last updated timestamp at {DateTime.Now}");
                    var rulesetsForClient = await client.GetRulesetsForClientAsync(codigaConfig.Rulesets);
                    IsInitializedWithRules = true;
                    if (rulesetsForClient == null)
                        return;

                    /*
                      If the server returns no rulesets, e.g. due to misconfiguration of codiga.yml,
                      we clear the cache. NOTE: this doesn't take into account if no ruleset is returned
                      due to an issue in how the Codiga server collects the rules.
                    */
                    if (rulesetsForClient.Count == 0)
                    {
                        ClearCache();
                        return;
                    }

                    UpdateCacheFrom(rulesetsForClient);
                    /*
                      Updating the local timestamp only if it has changed, because it may happen that
                      codiga.yml was updated locally with a non-existent ruleset, or a ruleset that has an earlier timestamp
                      than the latest updated one, so the rulesets configured don't result in an updated timestamp from the server.
                    */
                    Debug.WriteLine($"Fetching rulesets last updated timestamp at {DateTime.Now}");
                    long timestampFromServer = await client.GetRulesetsLastUpdatedTimestampAsync(codigaConfig.Rulesets);
                    if (timestampFromServer != RulesetslastUpdatedTimeStamp)
                        RulesetslastUpdatedTimeStamp = timestampFromServer;

                    //Only notify when not in testing mode
                    if (_clientProvider is DefaultCodigaClientProvider)
                        await NotifyActiveDocumentForTagUpdateAsync();
                }
                catch (CodigaAPIException)
                {
                    //Do nothing
                }
            }
            else
            {
                ClearCache();
            }
        }

        /// <summary>
        /// Handles the case when the codiga.yml file is unchanged, but there might be change on the server.
        /// </summary>
        private async Task UpdateCacheFromChangesOnServerAsync(ICodigaClient client)
        {
            if (CodigaConfig.Rulesets.Count == 0)
                return;

            try
            {
                //Retrieve the last updated timestamp for the rulesets
                Debug.WriteLine($"Fetching rulesets last updated timestamp at {DateTime.Now}");
                var timestampFromServer = await client.GetRulesetsLastUpdatedTimestampAsync(CodigaConfig.Rulesets.ToImmutableList());
                IsInitializedWithRules = true;
                //If there was a change on the server, we can get and cache the rulesets
                if (RulesetslastUpdatedTimeStamp != timestampFromServer)
                {
                    Debug.WriteLine($"Fetching rulesets at {DateTime.Now}");
                    var rulesetsForClient = await client.GetRulesetsForClientAsync(CodigaConfig.Rulesets.ToImmutableList());
                    if (rulesetsForClient == null)
                        return;

                    UpdateCacheFrom(rulesetsForClient);
                    RulesetslastUpdatedTimeStamp = timestampFromServer;
                    //Only notify when not in testing mode
                    if (_clientProvider is DefaultCodigaClientProvider)
                        await NotifyActiveDocumentForTagUpdateAsync();
                }
            }
            catch (CodigaAPIException)
            {
                //Do nothing
            }
        }

        /// <summary>
        /// Clears and repopulates this cache based on the argument rulesets' information returned
        /// from the Codiga API.
        /// <br/>
        /// Groups the rules by their target languages, converts them to <c>RosieRule</c> objects,
        /// and wraps and stores them in <see cref="RosieRulesCacheValue"/>s.
        /// </summary>
        /// <param name="rulesetsFromCodigaApi">the rulesets information</param>
        public void UpdateCacheFrom(IReadOnlyCollection<RuleSetsForClient> rulesetsFromCodigaApi)
        {
            var rulesByLanguage = rulesetsFromCodigaApi
                .Where(ruleset => ruleset.Rules != null)
                .SelectMany(ruleset => ruleset.Rules, (ruleset, rule) => new RuleWithNames(ruleset.Name, rule))
                .GroupBy(ruleWithName => ruleWithName.RosieRule.Language)
                .ToDictionary(entry =>
                {
                    Enum.TryParse<LanguageEnumeration>(entry.Key, out var language);
                    return language;
                }, entry => new RosieRulesCacheValue(entry.ToList()));

            //Clearing and repopulating the cache is easier than picking out one by one
            // the ones that remain, and the ones that have to be removed.
            _cachedRules.Clear();
            foreach (var keyValuePair in rulesByLanguage)
                _cachedRules.Add(keyValuePair.Key, keyValuePair.Value);
            
            CacheLastUpdatedTimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Gets the document that is currently active and focused, and if there is a <see cref="RosieViolationTagger"/>
        /// associated with it, it notifies that tagger to send that document for code analysis, and update tagging.
        /// </summary>
        private async Task NotifyActiveDocumentForTagUpdateAsync()
        {
            var activeDocumentView = await VS.Documents.GetActiveDocumentViewAsync();
            if (activeDocumentView?.TextView != null)
            {
                //If there is no tagger associated to the active view, we just don't do any update.
                //This can be the case e.g. when the language of the active file is not supported.
                if (activeDocumentView.TextView.Properties.ContainsProperty(typeof(RosieViolationTagger)))
                {
                    var tagger = activeDocumentView.TextView.Properties[typeof(RosieViolationTagger)] as RosieViolationTagger;
                    tagger?.UpdateAnnotationsAndNotifyTagsChangedAsync(activeDocumentView.TextView);
                }
            }
        }

        #endregion

        #region Get rules

        /// <summary>
        /// Returns the list of <see cref="RosieRule"/>s for the argument language and file path,
        /// that will be sent to the Rosie service for analysis.
        /// </summary>
        /// <param name="language">The language to get the rules for</param>
        /// <param name="pathOfAnalyzedFile">the absolute path of the file being analyzed.
        /// Required to pass in for the <c>ignore</c> configuration.</param>
        /// <param name="solutionDirectory">The solution root directory.
        /// Null only in case of production code, so we can retrieve the proper root directory.</param>
        /// <returns>The rules for the given language.</returns>
        public async Task<IReadOnlyList<RosieRule>> GetRosieRules(LanguageEnumeration language, string pathOfAnalyzedFile, string? solutionDirectory = null)
        {
            var solutionDir = solutionDirectory ?? await SolutionHelper.GetSolutionDir();
            if (solutionDir == null)
                return NoRule;
            
            var cachedLanguageType = GetCachedLanguageTypeOf(language);
            if (_cachedRules.ContainsKey(cachedLanguageType))
            {
                var cachedRules = _cachedRules[cachedLanguageType];
                var rosieRulesForLanguage = cachedRules != null ? cachedRules.RosieRules : NoRule;
                
                if (rosieRulesForLanguage.Count > 0)
                {
                    //Replaces backslash '\' symbols with forward slashes '/', so that in case of Windows specific paths,
                    // we still can compare the relative paths properly. 
                    string relativePathOfAnalyzedFile = pathOfAnalyzedFile.Replace(solutionDir, "").Replace("\\", "/");
                    
                    //Returns the RosieRules that either don't have an ignore rule, or their prefixes don't match the currently analyzed file's path
                    return rosieRulesForLanguage
                        .Where(rosieRule =>
                        {
                            //If there is no ruleset ignore or rule ignore for the current RosieRule, then we keep it/don't ignore it.
                            if (!CodigaConfig.Ignore.ContainsKey(rosieRule.RulesetName)
                                || !CodigaConfig.Ignore[rosieRule.RulesetName].RuleIgnores
                                    .ContainsKey(rosieRule.RuleName))
                                return true;
                                    
                            var ruleIgnore = CodigaConfig.Ignore[rosieRule.RulesetName].RuleIgnores[rosieRule.RuleName];

                            //If there is no prefix specified for the current rule ignore config,
                            // we don't keep the rule/ignore it.
                            if (ruleIgnore.Prefixes.Count == 0)
                                return false;

                            return ruleIgnore.Prefixes
                                    //Since the leading / is optional, we remove it
                                    .Select(RemoveLeadingSlash)
                                    //./, /. and .. sequences are not allowed in prefixes, therefore we consider them not matching the file path.
                                    //. symbols in general are allowed to be able to target exact file paths with their file extensions.
                                    .All(prefix =>
                                        prefix.Contains("..")
                                        || prefix.Contains("./")
                                        || prefix.Contains("/.")
                                        || !RemoveLeadingSlash(relativePathOfAnalyzedFile).StartsWith(prefix));
                        }).ToList();
                }
            }
            return NoRule;
        }
        
        private static string RemoveLeadingSlash(string path) {
            return path.StartsWith("/") ? path.Substring(1) : path;
        }
        
         /// <summary>
         /// Since, besides JavaScript files, rules for TypeScript files are also handled under the same JavaScript Rosie language
         /// type, we have to return JavaScript rules for TypeScript files as well. 
         /// </summary>
         /// <param name="fileLanguage">the file language to map</param>
         private static LanguageEnumeration GetCachedLanguageTypeOf(LanguageEnumeration fileLanguage) {
            return fileLanguage == LanguageEnumeration.Typescript ? LanguageEnumeration.Javascript : fileLanguage;
        }

        /// <summary>
        /// Returns the cached rules for the provided language and rule id.
        /// <br/>
        /// Null value for non-existent mapping for a language is already handled in <see cref="GetRosieRules"/>.
        /// <br/>
        /// It should not return null when retrieving the rule for the rule id, since in <c>RosieApiImpl#GetAnnotations()</c>
        /// the <see cref="RosieRuleResponse"/>s and their ids are based on the values cached here.
        /// </summary>
        public RuleWithNames GetRuleWithNamesFor(LanguageEnumeration language, string ruleId)
        {
            return _cachedRules[GetCachedLanguageTypeOf(language)].Rules[ruleId];
        }

        #endregion

        #region Disposal

        /// <summary>
        /// Empties the cache if it is not empty.
        /// </summary>
        private void ClearCache()
        {
            if (_cachedRules.Count > 0)
                _cachedRules.Clear();
            CodigaConfig = CodigaCodeAnalysisConfig.EMPTY;
            RulesetslastUpdatedTimeStamp = -1L;
        }

        /// <summary>
        /// Stops the cache updater background thread and disposes the cache.
        /// </summary>
        public static void Dispose()
        {
            Instance?._cancellationTokenSource?.Cancel();
            Instance?.ClearCache();
            IsInitializedWithRules = false;
            Instance = null;
        }

        #endregion

        public bool IsEmpty()
        {
            return _cachedRules.Count == 0;
        }
    }
}
