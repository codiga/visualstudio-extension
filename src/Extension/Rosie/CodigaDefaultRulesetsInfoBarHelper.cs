using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Extension.Caching;
using Extension.Settings;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Extension.Rosie
{
    /// <summary>
    /// Handles the analysis of the current solution/folder, and displays an info bar in the Solution Explorer when necessary.
    /// </summary>
    /// <seealso cref="https://learn.microsoft.com/en-us/visualstudio/extensibility/ux-guidelines/notifications-and-progress-for-visual-studio?view=vs-2022#BKMK_EmbeddedInfobar"/>
    /// <seealso cref="https://learn.microsoft.com/en-us/visualstudio/extensibility/vsix/recipes/notifications?view=vs-2022"/>
    /// <seealso cref="https://stackoverflow.com/questions/49278306/how-do-i-find-the-open-folder-in-a-vsix-extension"/>
    internal static class CodigaDefaultRulesetsInfoBarHelper
    {
        //\n characters are for better formatting of the entire content of the info bar
        private const string InfoBarText = "Check for security, code style in your Python code with Codiga.\n";
        private const string CreateCodigaYmlActionText = "Create codiga.yml\n";
        private const string NeverForThisSolutionActionText = "Never for this solution";

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
        ///     <li>At least one of the projects in the solution is a Python project.</li>
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
                && CodigaConfigFileUtil.FindCodigaConfigFile(serviceProvider) == null
                && await IsSolutionContainPythonProject(serviceProvider))
            {
                var model = new InfoBarModel(new[]
                {
                    new InfoBarTextSpan(InfoBarText),
                    new InfoBarHyperlink(CreateCodigaYmlActionText),
                    new InfoBarHyperlink(NeverForThisSolutionActionText)
                }, KnownMonikers.PlayStepGroup);

                var infoBar = await VS.InfoBar.CreateAsync(ToolWindowGuids80.SolutionExplorer, model);
                if (infoBar != null)
                {
                    infoBar.ActionItemClicked += InfoBar_ActionItemClicked;
                    await infoBar.TryShowInfoBarUIAsync();
                }

                infoBarHolder.InfoBar = infoBar;
                return;
            }

            infoBarHolder.InfoBar = null;
        }

        /// <summary>
        /// Handles when the user clicks one of the items in the info bar.
        /// </summary>
        private static void InfoBar_ActionItemClicked(object sender, InfoBarActionItemEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            switch (e.ActionItem.Text)
            {
                case CreateCodigaYmlActionText:
                    RecordCreateCodigaYaml();
                    CodigaConfigFileUtil.CreateCodigaConfigFile(VS.GetMefService<SVsServiceProvider>());
                    break;
                case NeverForThisSolutionActionText:
                    SolutionSettings.SaveNeverNotifyUserToCreateCodigaConfigFile(
                        VS.GetMefService<SVsServiceProvider>());
                    break;
            }

            //Whichever action is clicked, close the info bar
            e.InfoBarUIElement.Close();
        }

        private static void RecordCreateCodigaYaml()
        {
            var clientProvider = new DefaultCodigaClientProvider();
            if (!clientProvider.TryGetClient(out var client))
                return;

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

        /// <summary>
        /// Returns whether any of the projects in the current solution is a Python project.
        /// <br/>
        /// Python project guid comes from https://github.com/microsoft/PTVS/blob/main/Python/Product/VSCommon/CommonGuidList.cs
        /// </summary>
        /// <param name="serviceProvider">The service provider to retrieve information about the solution from.</param>
        private static async Task<bool> IsSolutionContainPythonProject(SVsServiceProvider serviceProvider)
        {
            var solution = await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            });

            solution.GetProperty((int)__VSPROPID7.VSPROPID_IsInOpenFolderMode, out object isInFolderMode);

            //If we have a proper VS solution open, check if at least one of the projects in it is a Python project
            if (!(bool)isInFolderMode)
            {
                var projectsInSolution =
                    ThreadHelper.JoinableTaskFactory.Run(async () => await VS.Solutions.GetAllProjectsAsync());
                return projectsInSolution.Any(project =>
                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                        await project.IsKindAsync("888888A0-9F3D-457C-B088-3A5042F75D52")));
            }

            return false;
        }
    }
}