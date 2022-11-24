using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Extension.Rosie.Model;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Extension.Rosie
{
    /// <summary>
    /// Default implementation of the Rosie client.
    /// </summary>
    public class RosieClient : IRosieClient
    {
        private const string RosiePostUrl = "https://analysis.codiga.io/analyze";
        private static readonly Regex AppVersionRegex = new Regex(@"(\d+)\.(\d+)\.\d+.*");
        private static readonly IList<RosieAnnotation> NoAnnotation = new List<RosieAnnotation>();

        /// <summary>
        /// Languages currently supported by Rosie.
        /// <br/>
        /// See also <see cref="RosieUtils.GetRosieLanguage"/>.
        /// </summary>
        private static readonly IList<LanguageUtils.LanguageEnumeration> SupportedLanguages =
            new List<LanguageUtils.LanguageEnumeration> { LanguageUtils.LanguageEnumeration.Python };

        /// <summary>
        /// In order to create the JSON property names as e.g. 'filename' as the server requires it,
        /// and not 'Filename' as they are required to be named in the model classes.
        /// </summary>
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly JsonSerializerOptions DeserializerOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };

        public async Task<IList<RosieAnnotation>> GetAnnotations(string file)
        {
            if (!File.Exists(file))
                return NoAnnotation;

            var language = LanguageUtils.Parse(Path.GetExtension(file));

            if (!SupportedLanguages.Contains(language))
                return NoAnnotation;

            try
            {
                var fileText = Encoding.UTF8.GetBytes(File.ReadAllText(file));
                var codeBase64 = Convert.ToBase64String(fileText);

                // Prepare the request
                var rosieRules = RosieRulesCache.Instance?.GetRosieRulesForLanguage(language);
                //If there is no rule for the target language, then Rosie is not called, and no annotation is performed
                if (rosieRules == null || rosieRules.Count == 0)
                    return NoAnnotation;

                using (var httpClient = new HttpClient())
                {
                    //Prepare the request and send it to the Rosie server
                    var rosieRequest = new RosieRequest(Path.GetFileName(file), RosieUtils.GetRosieLanguage(language),
                        "utf8",
                        codeBase64, rosieRules, true);
                    var userAgent = await GetUserAgent();
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
        /// Builds a user agent string from the current Visual Studio brand name and its major and minor version numbers.
        /// <br/>
        /// The solution is based on https://stackoverflow.com/a/58266774.
        /// <br/>
        /// See also: https://learn.microsoft.com/en-us/nuget/visual-studio-extensibility/nuget-api-in-visual-studio
        /// </summary>
        private static async Task<string> GetUserAgent()
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
            return $"{brandName ?? ""} {match.Groups[1].Value} {match.Groups[2].Value}";
        }
    }
}