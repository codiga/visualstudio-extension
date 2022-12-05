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
using EnvDTE;
using Extension.Caching;
using Extension.Rosie.Annotation;
using Extension.Rosie.Model;
using Extension.SnippetFormats;
using GraphQLClient;
using GraphQLClient.Model.Rosie;
using Microsoft.VisualStudio.Shell;
using Solution = EnvDTE.Solution;

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
        private DTE _dte;
        
        private static readonly TextWriterTraceListener TextWriterTraceListener = new TextWriterTraceListener(Console.Out);
        
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
        private IDictionary<LanguageUtils.LanguageEnumeration, RosieRulesCacheValue> _cachedRules;

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
        // internal DateTime ConfigFileLastWriteTime { get; set; } = DateTime.MinValue;
        internal long ConfigFileLastWriteTime { get; set; } = -1L;

        /// <summary>
        /// Ruleset names stored locally in the codiga.yml config file.
        /// </summary>
        public IList<string> RulesetNames { get; set; }

        /// <summary>
        /// The cache is considered initialized with rules right after the response is received from <see cref="ICodigaClient.GetRulesetsForClientAsync"/>,
        /// or when there is no <see cref="ICodigaClient"/> to use.
        /// </summary>
        public static bool IsInitializedWithRules;
        
        public static RosieRulesCache? Instance { get; set; }

        private Solution? _solution;
        private static string _testName;

        private RosieRulesCache()
        {
            _clientProvider = new DefaultCodigaClientProvider();
            _cachedRules = new ConcurrentDictionary<LanguageUtils.LanguageEnumeration, RosieRulesCacheValue>();
            RulesetNames = new SynchronizedCollection<string>();
        }
        
        //For testing
        private RosieRulesCache(Solution solution, ICodigaClientProvider clientProvider)
        {
            _solution = solution;
            _clientProvider = clientProvider;
            _cachedRules = new ConcurrentDictionary<LanguageUtils.LanguageEnumeration, RosieRulesCacheValue>();
            RulesetNames = new SynchronizedCollection<string>();
            Debug.Listeners.Add(TextWriterTraceListener);
            Debug.AutoFlush = true;
        }

        public static void Initialize()
        {
            Instance = new RosieRulesCache();
            Instance.StartPolling();
        }
        
        //For testing
        public static void Initialize(Solution solution, ICodigaClientProvider clientProvider, string testName)
        {
            Instance = new RosieRulesCache(solution, clientProvider);
            Debug.WriteLine($"Starting test {testName}");
            _testName = testName;
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
            var vssp = VS.GetMefService<SVsServiceProvider>();
            _dte = (_DTE)vssp.GetService(typeof(_DTE));
            
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
            NoCodigaClient, NoConfigFile, Success 
        }

        public async Task<UpdateResult> HandleCacheUpdateAsync()
        {
            Debug.WriteLine("Entered RosieRulesCache.HandleCacheUpdateAsync()");
            if (!_clientProvider.TryGetClient(out var client))
                return UpdateResult.NoCodigaClient;

            _solution ??= _dte.Solution;
            var codigaConfigFile = CodigaConfigFileUtil.FindCodigaConfigFile(_solution);

            if (codigaConfigFile == null || !File.Exists(codigaConfigFile))
            {
                Debug.WriteLine("Didn't find config file.");
                ClearCache();
                //Since the config file no longer exists, its last write time is reset too
                ConfigFileLastWriteTime = -1L;
                IsInitializedWithRules = true;
                Debug.WriteLine("Cleared cache in HandleCacheUpdateAsync().");
                return UpdateResult.NoConfigFile;
            }

            var currentLastWriteTime = new DateTimeOffset(File.GetLastWriteTime(codigaConfigFile)).ToUnixTimeMilliseconds();
            Debug.WriteLine($"Config file last write time: cached [{ConfigFileLastWriteTime}], current: [{currentLastWriteTime}]");
            //If the Codiga config file has changed (its last write time doesn't match its previous write time)
            if (ConfigFileLastWriteTime != currentLastWriteTime)
            // if (ConfigFileLastWriteTime.CompareTo(File.GetLastWriteTime(codigaConfigFile)) != 0)
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
            Debug.WriteLine("Entered RosieRulesCache.UpdateCacheFromModifiedCodigaConfigFileAsync()");
            // ConfigFileLastWriteTime = File.GetLastWriteTime(codigaConfigFile);
            ConfigFileLastWriteTime =
                new DateTimeOffset(File.GetLastWriteTime(codigaConfigFile)).ToUnixTimeMilliseconds();
            var rawCodigaConfig = File.ReadAllText(codigaConfigFile);
            var rulesetNames = CodigaConfigFileUtil.DeserializeConfig(rawCodigaConfig)?.GetRulesets();
            //If the config file is not configured properly, we clear the cache
            if (rulesetNames == null)
            {
                Debug.WriteLine("Could not deserialize ruleset names. Clearing the cache in UpdateCacheFromModifiedCodigaConfigFile().");
                ClearCache();
                return;
            }
            
            RulesetNames = rulesetNames;

            //If there is at least on ruleset name, we can make a request with them
            if (RulesetNames.Count > 0)
            {
                Debug.WriteLine("Found more than 1 rulesets.");
                foreach (var rulesetName in RulesetNames)
                    Debug.WriteLine($"Found ruleset name: {rulesetName}");
                
                try
                {
                    var rulesetsForClient = await client.GetRulesetsForClientAsync(rulesetNames);
                    IsInitializedWithRules = true;
                    if (rulesetsForClient == null)
                    {
                        Debug.WriteLine("Returned null rulesetsForClient from server.");
                        return;
                    }

                    /*
                      If the server returns no rulesets, e.g. due to misconfiguration of codiga.yml,
                      we clear the cache. NOTE: this doesn't take into account if no ruleset is returned
                      due to an issue in how the Codiga server collects the rules.
                    */
                    if (rulesetsForClient.Count == 0)
                    {
                        Debug.WriteLine("Returned empty rulesetsForClient from server. Clearing the cache.");
                        ClearCache();
                        return;
                    }

                    Debug.WriteLine("Updating the cache after successful rulesetsForClient from server.");
                    UpdateCacheFrom(rulesetsForClient);
                    Debug.WriteLine("Updated the cache");
                    /*
                      Updating the local timestamp only if it has changed, because it may happen that
                      codiga.yml was updated locally with a non-existent ruleset, or a ruleset that has an earlier timestamp
                      than the latest updated one, so the rulesets configured don't result in an updated timestamp from the server.
                    */
                    long timestampFromServer = await client.GetRulesetsLastUpdatedTimestampAsync(rulesetNames);
                    if (timestampFromServer != RulesetslastUpdatedTimeStamp)
                    {
                        Debug.WriteLine("Saving the updated RulesetslastUpdatedTimeStamp.");
                        RulesetslastUpdatedTimeStamp = timestampFromServer;
                    }
                    else
                    {
                        Debug.WriteLine("Not saving same RulesetslastUpdatedTimeStamp.");
                    }

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
                Debug.WriteLine("Found empty ruleset names. Clearing the cache in UpdateCacheFromModifiedCodigaConfigFile().");
                ClearCache();
            }
        }

        /// <summary>
        /// Handles the case when the codiga.yml file is unchanged, but there might be change on the server.
        /// </summary>
        private async Task UpdateCacheFromChangesOnServerAsync(ICodigaClient client)
        {
            Debug.WriteLine("Entered RosieRulesCache.UpdateCacheFromChangesOnServerAsync()");
            if (RulesetNames.Count == 0)
            {
                Debug.WriteLine("There was no ruleset name.");
                return;
            }
            
            try
            {
                //Retrieve the last updated timestamp for the rulesets
                var timestampFromServer = await client.GetRulesetsLastUpdatedTimestampAsync(RulesetNames.ToImmutableList());
                IsInitializedWithRules = true;
                //If there was a change on the server, we can get and cache the rulesets
                if (RulesetslastUpdatedTimeStamp != timestampFromServer)
                {
                    Debug.WriteLine("Timestamp was different on the server than locally.");
                    var rulesetsForClient = await client.GetRulesetsForClientAsync(RulesetNames.ToImmutableList());
                    if (rulesetsForClient == null)
                        return;

                    Debug.WriteLine("Updating the cache after successful rulesetsForClient from server (UpdateCacheFromChangesOnServerAsync).");
                    UpdateCacheFrom(rulesetsForClient);
                    Debug.WriteLine("Updated the cache (UpdateCacheFromChangesOnServerAsync)");
                    RulesetslastUpdatedTimeStamp = timestampFromServer;
                    //Only notify when not in testing mode
                    if (_clientProvider is DefaultCodigaClientProvider)
                        await NotifyActiveDocumentForTagUpdateAsync();
                }
                else
                {
                    Debug.WriteLine("Timestamp on server was the same the local value.");
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
        private void UpdateCacheFrom(IReadOnlyCollection<RuleSetsForClient> rulesetsFromCodigaApi)
        {
            Debug.WriteLine("Entered RosieRulesCache.UpdateCacheFrom()");
            
            var rulesByLanguage = rulesetsFromCodigaApi
                .Where(ruleset => ruleset.Rules != null)
                .SelectMany(ruleset => ruleset.Rules, (ruleset, rule) =>
                {
                    Debug.WriteLine($"Converting {ruleset.Name}/{rule.Name} to RuleWithNames.");
                    return new RuleWithNames(ruleset.Name, rule);
                })
                .GroupBy(ruleWithName => ruleWithName.RosieRule.Language)
                .ToDictionary(entry =>
                {
                    Enum.TryParse<LanguageUtils.LanguageEnumeration>(entry.Key, out var language);
                    return language;
                }, entry => new RosieRulesCacheValue(entry.ToList()));

            if (rulesByLanguage.Count == 0)
            {
                Debug.WriteLine("Grouping by language resulted in empty rules dictionary.");
            }
            //Clearing and repopulating the cache is easier than picking out one by one
            // the ones that remain, and the ones that have to be removed.
            _cachedRules.Clear();
            foreach (var keyValuePair in rulesByLanguage)
            {
                _cachedRules.Add(keyValuePair.Key, keyValuePair.Value);
                Debug.WriteLine($"Added entry for {keyValuePair.Key.GetName()}");
            }
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
        /// Returns the <see cref="RosieRule"/>s for the provided language.
        /// </summary>
        /// <param name="language">The language to get the rules for.</param>
        /// <returns>The rules for the given language.</returns>
        public IReadOnlyList<RosieRule> GetRosieRulesForLanguage(LanguageUtils.LanguageEnumeration language)
        {
            if (_cachedRules.ContainsKey(language))
            {
                var cachedRules = _cachedRules[language];
                return cachedRules != null ? cachedRules.RosieRules : NoRule;                
            }
            return NoRule;
        }

        /// <summary>
        /// Returns the cached rules for the provided language and rule id.
        /// <br/>
        /// Null value for non-existent mapping for a language is already handled in <see cref="GetRosieRulesForLanguage"/>.
        /// <br/>
        /// It should not return null when retrieving the rule for the rule id, since in <c>RosieApiImpl#GetAnnotations()</c>
        /// the <see cref="RosieRuleResponse"/>s and their ids are based on the values cached here.
        /// </summary>
        public RuleWithNames GetRuleWithNamesFor(LanguageUtils.LanguageEnumeration language, string ruleId)
        {
            return _cachedRules[language].Rules[ruleId];
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
            if (RulesetNames.Count > 0)
                RulesetNames.Clear();
            RulesetslastUpdatedTimeStamp = -1L;
        }

        /// <summary>
        /// Stops the cache updater background thread and disposes the cache.
        /// </summary>
        public static void Dispose()
        {
            Instance?._cancellationTokenSource?.Cancel();
            Instance?.ClearCache();
            Instance = null;
            Debug.WriteLine($"Finishes test {_testName}");
            Debug.WriteLine("");
            Debug.Listeners.Remove(TextWriterTraceListener);
        }

        #endregion

        public bool IsEmpty()
        {
            return _cachedRules.Count == 0;
        }
    }
}
