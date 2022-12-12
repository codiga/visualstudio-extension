using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Extension.Rosie.Model;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Extension.Rosie
{
    /// <summary>
    /// Default implementation of the Rosie client.
    /// </summary>
    public class RosieClient : IRosieClient
    {
        /// <summary>
        /// An empty list of <c>RosieAnnotation</c>s, so that when we need an empty list of them, we don't need to create a new list each time.
        /// </summary>
        public static readonly IList<RosieAnnotation> NoAnnotation = new List<RosieAnnotation>();
        
        /// <summary>
        /// Languages currently supported by Rosie.
        /// <br/>
        /// See also <see cref="RosieUtils.GetRosieLanguage"/>.
        /// </summary>
        private static readonly IList<LanguageUtils.LanguageEnumeration> SupportedLanguages =
            new List<LanguageUtils.LanguageEnumeration> { LanguageUtils.LanguageEnumeration.Python };

        /// <summary>
        /// Matches for example '17.4.33103.184 D17.4' where majorVersion is 17, minorVersion is 4.
        /// </summary>
        private static readonly Regex AppVersionRegex = new Regex(@"(?<majorVersion>\d+)\.(?<minorVersion>\d+)\.\d+.*");

        /// <summary>
        /// In order to create the JSON property names as e.g. 'filename' as the server requires it,
        /// and not 'Filename' as they are required to be named in the model classes.
        /// </summary>
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Essentially, to handle the opposite of <c>SerializerOptions</c>, to be able to deserialize
        /// into the fields of <see cref="RosieResponse"/> using the uppercase field names.
        /// </summary>
        private static readonly JsonSerializerOptions DeserializerOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };

        private const string RosiePostUrl = "https://analysis.codiga.io/analyze";

        private readonly TextBufferDataProvider _dataProvider;

        public RosieClient()
        {
            _dataProvider = new TextBufferDataProvider();
        }

        //For testing
        public RosieClient(Func<ITextBuffer, string?> fileNameProvider, Func<ITextBuffer, string> fileTextProvider)
        {
            _dataProvider = new TextBufferDataProvider
            {
                FileName = fileNameProvider,
                FileText = fileTextProvider,
                IsTestMode = true
            };
        }

        public /*override*/ async Task<IList<RosieAnnotation>> GetAnnotations(ITextBuffer textBuffer)
        {
            var fileName = _dataProvider.FileName(textBuffer);
            if (fileName == null || !File.Exists(fileName))
                return NoAnnotation;

            var language = LanguageUtils.ParseFromFileName(fileName);

            if (!SupportedLanguages.Contains(language))
                return NoAnnotation;

            try
            {
                //The ITextBuffer's text contains \r\n new line symbols, but sending the file content having the \r characters
                // included, can result in incorrect start/end line/column offsets to be returned from Rosie.
                var fileText = Encoding.UTF8.GetBytes(_dataProvider.FileText(textBuffer).Replace("\r", ""));
                var codeBase64 = Convert.ToBase64String(fileText);

                await InitializeRulesCacheIfNeeded();

                //Prepare the request
                var rosieRules = RosieRulesCache.Instance?.GetRosieRulesForLanguage(language);
                //If there is no rule for the target language, then Rosie is not called, and no tagging is performed
                if (rosieRules == null || rosieRules.Count == 0)
                    return NoAnnotation;

                using (var httpClient = new HttpClient())
                {
                    //Prepare the request and send it to the Rosie server
                    var rosieRequest = new RosieRequest(Path.GetFileName(fileName),
                        RosieUtils.GetRosieLanguage(language),
                        "utf8",
                        codeBase64, rosieRules, true);
                    var userAgent = await GetUserAgentAsync();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
                    var requestBody = JsonSerializer.Serialize(rosieRequest, SerializerOptions);
                    var requestContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    var httpResponseMessage = await httpClient.PostAsync(RosiePostUrl, requestContent);

                    var annotations = NoAnnotation;
                    if (httpResponseMessage.Content is StreamContent content)
                    {
                        var responseBody = await content.ReadAsStreamAsync();
                        var rosieResponse =
                            await JsonSerializer.DeserializeAsync<RosieResponse>(responseBody, DeserializerOptions);

                        annotations = rosieResponse?.RuleResponses
                            .SelectMany(res =>
                            {
                                //This is a workaround of calling Distinct(), because Distinct() seems to work based on
                                // hashcode, instead of Equals(), and that doesn't filter out duplicate elements.
                                var distinct = new List<RosieViolation>();
                                foreach (var vi in res.Violations)
                                    if (!distinct.Any(v => v.Equals(vi)))
                                        distinct.Add(vi);

                                return distinct
                                    .Select(violation =>
                                    {
                                        var rule = RosieRulesCache.Instance?.GetRuleWithNamesFor(language,
                                            res.Identifier);
                                        return new RosieAnnotation(rule.RuleName, rule.RulesetName, violation);
                                    });
                            })
                            .ToList();
                    }

                    return annotations ?? NoAnnotation;
                }
            }
            catch
            {
                return NoAnnotation;
            }
        }

        /// <summary>
        /// Initializes the rules cache if it hasn't been done.
        /// <br/>
        /// We are doing this at the first request for annotations (see <see cref="GetAnnotations"/> above), instead of on/after Solution open,
        /// since a document might be open (thus has an <c>ITextView</c> and <c>ITextBuffer</c>, and a <see cref="RosieViolationTagger"/> is created)
        /// before the Solution would open or complete opening. 
        /// </summary>
        private async Task InitializeRulesCacheIfNeeded()
        {
            if (RosieRulesCache.Instance == null)
            {
                if (!_dataProvider.IsTestMode)
                {
                    //Temporarily switching back to main thread due to RosieRulesCache.StartPolling()
                    await Task.Run(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        RosieRulesCache.Initialize();
                    });

                    //Wait a little for the RosieRulesCache to be initialized with rules.
                    await Task.Run(async () =>
                    {
                        while (true)
                        {
                            if (!RosieRulesCache.IsInitializedWithRules)
                            {
                                var delay = Task.Delay(TimeSpan.FromMilliseconds(100), new CancellationToken());
                                try
                                {
                                    await delay;
                                }
                                catch (TaskCanceledException)
                                {
                                    return;
                                }
                            }
                            else
                                return;
                        }
                    }).WithTimeout(TimeSpan.FromSeconds(2));
                }
                else
                {
                    RosieRulesCache.Initialize(false);
                    RosieRulesCache.IsInitializedWithRules = true;
                }
            }
        }

        /// <summary>
        /// Builds a user agent string from the current Visual Studio brand name and its major and minor version numbers.
        /// <br/>
        /// The solution is based on https://stackoverflow.com/a/58266774.
        /// <br/>
        /// See also: https://learn.microsoft.com/en-us/nuget/visual-studio-extensibility/nuget-api-in-visual-studio
        /// </summary>
        private static async Task<string> GetUserAgentAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var vssp = VS.GetMefService<SVsServiceProvider>();
            var shell = vssp.GetService(typeof(SVsShell)) as IVsShell;

            //e.g. 17.4.33103.184 D17.4
            object? version = null;
            shell?.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out version);

            //e.g. "Microsoft Visual Studio Community 2022"
            object? brandName = null;
            shell?.GetProperty((int)__VSSPROPID5.VSSPROPID_AppBrandName, out brandName);

            if (version == null)
                return brandName?.ToString() ?? "";

            var match = AppVersionRegex.Match((string)version);
            //e.g. Microsoft Visual Studio Community 2022 17 4
            return $"{brandName ?? ""} {match.Groups["majorVersion"].Value} {match.Groups["minorVersion"].Value}";
        }

        /// <summary>
        /// Returns whether the language of the provided filename is supported by Rosie.
        /// </summary>
        /// <param name="fileName">The file name to validate the language of.</param>
        /// <returns>True if the file language is supported, false otherwise.</returns>
        public static bool IsLanguageOfFileSupported(string? fileName)
        {
            if (fileName == null)
                return false;

            var languageOfCurrentFile = LanguageUtils.ParseFromFileName(fileName);
            return SupportedLanguages.Contains(languageOfCurrentFile);
        }
    }
}