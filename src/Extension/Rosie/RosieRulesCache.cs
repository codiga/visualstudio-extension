using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Extension.Caching;
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
        private const int PollIntervalInMillis = 10000;
        private readonly IReadOnlyList<RosieRule> NoRule = new List<RosieRule>(); 

        private ICodigaClientProvider _clientProvider;
        private CancellationTokenSource _cancellationTokenSource;
        private DTE _dte;
        
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
        private long _lastUpdatedTimeStamp = -1L;

        /// <summary>
        /// DateTime.MinValue means the last write time of codiga.yml hasn't been set, or there is no codiga.yml file in the Solution root.
        /// </summary>
        private DateTime _configFileLastWriteTime = DateTime.MinValue;

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

        private RosieRulesCache()
        {
            _clientProvider = new DefaultCodigaClientProvider();
            _cachedRules = new ConcurrentDictionary<LanguageUtils.LanguageEnumeration, RosieRulesCacheValue>();
            RulesetNames = new SynchronizedCollection<string>();
        }
        
        //For testing
        public RosieRulesCache(Solution solution, ICodigaClientProvider clientProvider)
        {
            _solution = solution;
            _clientProvider = clientProvider;
            _cachedRules = new ConcurrentDictionary<LanguageUtils.LanguageEnumeration, RosieRulesCacheValue>();
            RulesetNames = new SynchronizedCollection<string>();
        }

        public static void Initialize()
        {
            Instance = new RosieRulesCache();
            Instance.StartPolling();
        }
        
        //For testing
        public static void Initialize(Solution solution, ICodigaClientProvider clientProvider)
        {
            Instance = new RosieRulesCache(solution, clientProvider);
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
                switch (HandleCacheUpdate())
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

        public UpdateResult HandleCacheUpdate()
        {
            if (!_clientProvider.TryGetClient(out var client))
                return UpdateResult.NoCodigaClient;

            _solution ??= _dte.Solution;
            var codigaConfigFile = CodigaConfigFileUtil.FindCodigaConfigFile(_solution);

            if (!File.Exists(codigaConfigFile))
            {
                ClearCache();
                //Since the config file no longer exists, its last write time is reset too
                _configFileLastWriteTime = DateTime.MinValue;
                IsInitializedWithRules = true;
                return UpdateResult.NoConfigFile;
            }

            //If the Codiga config file has changed (its last write time doesn't match its previous write time)
            if (_configFileLastWriteTime.CompareTo(File.GetLastWriteTime(codigaConfigFile)) != 0)
                UpdateCacheFromModifiedCodigaConfigFile(codigaConfigFile, client);
            else
                UpdateCacheFromChangesOnServer(client);
            
            return UpdateResult.Success;
        }

        /// <summary>
        /// Handles when there was a change in the codiga.yml file.
        /// </summary>
        private async void UpdateCacheFromModifiedCodigaConfigFile(string? codigaConfigFile, ICodigaClient client)
        {
            if (codigaConfigFile == null)
                return;
            
            _configFileLastWriteTime = File.GetLastWriteTime(codigaConfigFile);
            var rawCodigaConfig = File.ReadAllText(codigaConfigFile);
            var rulesetNames = CodigaConfigFileUtil.DeserializeConfig(rawCodigaConfig)?.Rulesets;
            if (rulesetNames == null)
                return;

            RulesetNames = rulesetNames;

            //If there is at least on ruleset name, we can make a request with them
            if (RulesetNames.Count > 0)
            {
                try
                {
                    var rulesetsForClient = await client.GetRulesetsForClientAsync(rulesetNames);
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
                    long timestampFromServer = await client.GetRulesetsLastUpdatedTimestampAsync(rulesetNames);
                    if (timestampFromServer != _lastUpdatedTimeStamp)
                        _lastUpdatedTimeStamp = timestampFromServer;
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
        /// Clears and repopulates this cache based on the argument rulesets' information returned
        /// from the Codiga API.
        /// <br/>
        /// Groups the rules by their target languages, converts them to <c>RosieRule</c> objects,
        /// and wraps and stores them in <see cref="RosieRulesCacheValue"/>s.
        /// </summary>
        /// <param name="rulesetsFromCodigaApi">the rulesets information</param>
        private void UpdateCacheFrom(IReadOnlyCollection<RuleSetsForClient> rulesetsFromCodigaApi)
        {
            var rulesByLanguage = rulesetsFromCodigaApi
                .Where(ruleset => ruleset.Rules != null)
                .SelectMany(ruleset => ruleset.Rules, (ruleset, rule) => new RuleWithNames(ruleset.Name, rule))
                .GroupBy(ruleWithName => ruleWithName.RosieRule.Language)
                .ToDictionary(entry =>
                {
                    Enum.TryParse<LanguageUtils.LanguageEnumeration>(entry.Key, out var language);
                    return language;
                }, entry => new RosieRulesCacheValue(entry.ToList()));

            //Clearing and repopulating the cache is easier than picking out one by one
            // the ones that remain, and the ones that have to be removed.
            _cachedRules.Clear();
            foreach (var keyValuePair in rulesByLanguage)
                _cachedRules.Add(keyValuePair.Key, keyValuePair.Value);
        }

        /// <summary>
        /// Handles the case when the codiga.yml file is unchanged, but there might be change on the server.
        /// </summary>
        private async void UpdateCacheFromChangesOnServer(ICodigaClient client)
        {
            if (RulesetNames.Count == 0)
                return;

            try
            {
                //Retrieve the last updated timestamp for the rulesets
                var timestampFromServer = await client.GetRulesetsLastUpdatedTimestampAsync(RulesetNames.ToImmutableList());
                IsInitializedWithRules = true;
                //If there was a change on the server, we can get and cache the rulesets
                if (_lastUpdatedTimeStamp != timestampFromServer)
                {
                    var rulesetsForClient = await client.GetRulesetsForClientAsync(RulesetNames.ToImmutableList());
                    if (rulesetsForClient == null)
                        return;

                    UpdateCacheFrom(rulesetsForClient);
                    _lastUpdatedTimeStamp = timestampFromServer;
                }
            }
            catch (CodigaAPIException)
            {
                //Do nothing
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
            _lastUpdatedTimeStamp = -1L;
        }

        /// <summary>
        /// Stops the cache updater background thread and disposes the cache.
        /// </summary>
        public static void Dispose()
        {
            Instance?._cancellationTokenSource?.Cancel();
            Instance?.ClearCache();
            Instance = null;
        }

        #endregion
    }
}
