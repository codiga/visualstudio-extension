using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Extension.Caching;
using Extension.Helpers;
using Extension.Settings;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using LanguageEnumeration = Extension.SnippetFormats.LanguageUtils.LanguageEnumeration;

namespace Extension.Rosie
{
    /// <summary>
    /// Handles the analysis of the current solution/folder, and displays an info bar in the Solution Explorer when necessary.
    /// </summary>
    /// <seealso cref="https://learn.microsoft.com/en-us/visualstudio/extensibility/ux-guidelines/notifications-and-progress-for-visual-studio?view=vs-2022#BKMK_EmbeddedInfobar"/>
    /// <seealso cref="https://learn.microsoft.com/en-us/visualstudio/extensibility/vsix/recipes/notifications?view=vs-2022"/>
    /// <seealso cref="https://stackoverflow.com/questions/49278306/how-do-i-find-the-open-folder-in-a-vsix-extension"/>
    public static class CodigaDefaultRulesetsInfoBarHelper
    {
        //\n characters are for better formatting of the entire content of the info bar
        private const string InfoBarText = "Check for security, code style in your Python code with Codiga.\n";
        private const string CreateCodigaYmlActionText = "Create codiga.yml\n";
        private const string NeverForThisSolutionActionText = "Never for this solution";

        private static readonly string[] FileSearchPatterns = { "*.py", "*.js", "*.jsx", "*.ts", "*.tsx" };
        private static readonly string[] FolderNamesToExclude = { ".vs", ".vscode", ".idea", ".git", "node_modules" };

        /// <summary>
        /// Holds and instance of <see cref="InfoBar"/> that is saved in <see cref="ShowDefaultRulesetCreationInfoBarAsync"/>.
        /// <br/>
        /// When one opens a solution/folder for which the info bar is displayed, then opens another solution/folder,
        /// the following can happen:
        /// <ul>
        ///     <li>if the newly opened solution wouldn't show the info bar, the previous solution's info bar is still displayed,</li>
        ///     <li>if the newly opened solution would show the info bar, both the previous and the new solution's info bar is displayed.</li>
        /// </ul>
        /// <br/>
        /// Therefore, when a solution/folder gets closed we have to close the info bar, which happens in <see cref="ExtensionPackage.CleanupCachesAndServices"/>.
        /// <br/>
        /// This wrapper is used to keep the async nature of <see cref="ShowDefaultRulesetCreationInfoBarAsync"/>, and because async methods
        /// are not allowed to have <c>out</c> parameters.
        /// </summary>
        internal class InfoBarHolder
        {
            internal InfoBar? InfoBar { get; set; }
        }

        /// <summary>
        /// Displays an info bar in the Solution Explorer with the following options:
        /// <ul>
        ///     <li>Create codiga.yml</li>
        ///     <li>Never for this solution</li>
        /// </ul>
        /// <br/>
        /// The info bar is displayed when all the following conditions are met:
        /// <ol>
        ///     <li>The user hasn't clicked the <strong>Never for this solution</strong> option.</li>
        ///     <li>There is no Codiga config file in the solution root/open folder.</li>
        ///     <li>At least one of the projects in the solution is a Python project, or the folder contains at least
        ///     one file whose language is supported by Rosie.</li>
        /// </ol>
        /// </summary>
        internal static async void ShowDefaultRulesetCreationInfoBarAsync(InfoBarHolder infoBarHolder)
        {
            var serviceProvider = await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return await VS.GetMefServiceAsync<SVsServiceProvider>();
            });

            if (SolutionSettings.IsShouldNotifyUserToCreateCodigaConfig(serviceProvider)
                && CodigaConfigFileUtil.FindCodigaConfigFile(serviceProvider) == null)
            {
                var supportedLanguage = await GetLanguageOfSupportedProjectOrFileAsync(serviceProvider);
                if (supportedLanguage == LanguageEnumeration.Unknown)
                    return;

                var model = new InfoBarModel(new[]
                {
                    new InfoBarTextSpan(InfoBarText),
                    new InfoBarHyperlink(CreateCodigaYmlActionText),
                    new InfoBarHyperlink(NeverForThisSolutionActionText)
                }, KnownMonikers.PlayStepGroup);

                var infoBar = await VS.InfoBar.CreateAsync(ToolWindowGuids80.SolutionExplorer, model);
                if (infoBar == null)
                    return;

                //Handles when the user clicks one of the items in the info bar.
                // Implemented as an inline event handler, so that the supported language can be passed to determine
                // the default ruleset config.
                infoBar.ActionItemClicked += (_, args) =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();

                    switch (args.ActionItem.Text)
                    {
                        case CreateCodigaYmlActionText:
                            RecordCreateCodigaYaml();
                            CodigaConfigFileUtil.CreateCodigaConfigFile(
                                supportedLanguage,
                                VS.GetMefService<SVsServiceProvider>());
                            break;
                        case NeverForThisSolutionActionText:
                            SolutionSettings.SaveNeverNotifyUserToCreateCodigaConfigFile(
                                VS.GetMefService<SVsServiceProvider>());
                            break;
                    }

                    //Whichever action is clicked, close the info bar
                    args.InfoBarUIElement.Close();
                };

                await infoBar.TryShowInfoBarUIAsync();

                infoBarHolder.InfoBar = infoBar;
                return;
            }

            infoBarHolder.InfoBar = null;
        }

        private static void RecordCreateCodigaYaml()
        {
            if (new DefaultCodigaClientProvider().TryGetClient(out var client))
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    try
                    {
                        await client.RecordCreateCodigaYaml();
                    }
                    catch
                    {
                        //Even if recording this metric fails, the Codiga config file must be created
                    }
                });
            }
        }

        /// <summary>
        /// Returns whether any of the projects in the current solution is a Python project,
        /// or if in Open Folder mode, whether the folder or any of its sub-folders contain a Python file.
        /// <br/>
        /// Python project guid comes from https://github.com/microsoft/PTVS/blob/main/Python/Product/VSCommon/CommonGuidList.cs
        /// </summary>
        /// <param name="serviceProvider">The service provider to retrieve information about the solution from.</param>
        private static async Task<LanguageEnumeration> GetLanguageOfSupportedProjectOrFileAsync(
            SVsServiceProvider serviceProvider)
        {
            var solution = await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            });

            //If we have a proper VS solution open, check if at least one of the projects in it is a Python project
            return !SolutionHelper.IsInOpenFolderMode(solution) && IsContainPythonProject()
                ? LanguageEnumeration.Python
                //Running the lookup in the background, so it doesn't block the UI
                : await Task.Run(() => FindSupportedFile(SolutionHelper.GetSolutionDir(serviceProvider)));
        }

        private static bool IsContainPythonProject()
        {
            var projectsInSolution =
                ThreadHelper.JoinableTaskFactory.Run(async () => await VS.Solutions.GetAllProjectsAsync());
            return projectsInSolution.Any(project =>
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                    await project.IsKindAsync("888888A0-9F3D-457C-B088-3A5042F75D52")));
        }

        /// <summary>
        /// Searches the argument <c>directory</c> and all its sub-directories for the presence of files with any
        /// of the supported languages.
        /// <br/>
        /// It excludes lookup in some folders listed in <see cref="FolderNamesToExclude"/>.
        /// </summary>
        /// <param name="directory">The root directory to search in</param>
        /// <returns>The language of the found file if there is a file found, or 'Unknown' if no file with supported language was found.</returns>
        public static LanguageEnumeration FindSupportedFile(string directory)
        {
            try
            {
                //Composes a parallel query that goes through the file extension search patterns of the currently supported languages,
                //and returns the language of the file that it finds in the current directory, or 'Unknown' if there is no supported file in there.
                var foundLanguage = FileSearchPatterns.AsParallel().Select(searchPattern =>
                {
                    //Using foreach instead of a call to '.Any()' because 'Any()' creates an extra enumerator each time it is called.
                    foreach (var _ in Directory.EnumerateFiles(directory, searchPattern, SearchOption.TopDirectoryOnly))
                    {
                        switch (searchPattern)
                        {
                            case "*.py":
                                return LanguageEnumeration.Python;
                            case "*.js":
                            case "*.jsx":
                                return LanguageEnumeration.Javascript;
                            case "*.ts":
                            case "*.tsx":
                                return LanguageEnumeration.Typescript;
                        }
                    }

                    return LanguageEnumeration.Unknown;
                });

                //Evaluates the previous query and if it contains any of the currently supported languages,
                //this method returns that language.
                using (var enumerator = foundLanguage.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                        if (LanguageEnumeration.Unknown != enumerator.Current)
                            return enumerator.Current;
                }

                //If there is no file with supported language is found in the current directory,
                //start to iterate through the files in the subdirectories.
                foreach (var subDir in Directory.EnumerateDirectories(directory))
                {
                    //Exclude non-existent folders, and ones whose name starts with a dot
                    if (subDir != null)
                    {
                        string? directoryName = Path.GetDirectoryName(subDir);
                        if (!FolderNamesToExclude.Contains(directoryName))
                        {
                            var supportedLanguage = FindSupportedFile(subDir);
                            if (supportedLanguage != LanguageEnumeration.Unknown)
                                return supportedLanguage;
                        }
                    }
                }
            }
            catch
            {
                //Falling through to return null, so that we return to one level up in the recursion,
                // with no specific file found
            }

            return LanguageEnumeration.Unknown;
        }
    }
}