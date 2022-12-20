using System.IO;
using System.Linq;
using Extension.Helpers;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;

namespace Extension.Settings
{
    /// <summary>
    /// Provides functionality to save solution specific configuration (as opposed to application specific ones)
    /// for anything related to Codiga.
    /// <br/>
    /// The settings file is saved in the <c>{solution_root}\.vs</c> directory that is created automatically for each Visual Studio solution,
    /// or when a non-solution project folder is opened, so that users don't have to explicitly deal with this config file,
    /// for example .gitignore it, because it will be out of sight. 
    /// </summary>
    public static class SolutionSettings
    {
        private const string SolutionSettingsFileName = "CodigaSolutionSettings.json";

        /// <summary>
        /// Retrieves whether the user has already chosen to never be notified about the creation of
        /// the Codiga config file.
        /// </summary>
        /// <param name="serviceProvider">The service provider to retrieve information about the solution from.</param>
        /// <returns>True if the user should be notified, false otherwise.</returns>
        internal static bool IsShouldNotifyUserToCreateCodigaConfig(SVsServiceProvider serviceProvider)
        {
            var solutionSettingsFile = FindSolutionSettingsFile(serviceProvider);
            if (solutionSettingsFile == null)
                return true;

            try
            {
                var solutionSettings =
                    JsonConvert.DeserializeObject<SolutionSettingsFile>(File.ReadAllText(solutionSettingsFile));
                return solutionSettings?.ShouldNotifyUserToCreateCodigaConfig ?? true;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Called when the user choses the <c>Never for this solution</c> option in the related info bar.
        /// <br/>
        /// It saves the user's choice to no longer show the info bar regarding the Codiga config creation.
        /// </summary>
        internal static void SaveNeverNotifyUserToCreateCodigaConfigFile(
            SVsServiceProvider serviceProvider)
        {
            var dotVsDirectory = $"{SolutionHelper.GetSolutionDir(serviceProvider)}\\.vs";
            if (!Directory.Exists(dotVsDirectory))
                Directory.CreateDirectory(dotVsDirectory);
                
            File.WriteAllText(
                $"{dotVsDirectory}\\{SolutionSettingsFileName}",
                JsonConvert.SerializeObject(new SolutionSettingsFile
                {
                    ShouldNotifyUserToCreateCodigaConfig = false
                }));
        }

        /// <summary>
        /// Looks up the Codiga solution settings file in the <c>.vs</c> directory located in the provided Solution's root directory,
        /// or in the currently open folder.
        /// </summary>
        /// <param name="serviceProvider">The service provider to retrieve information about the solution from.</param>
        /// <returns>The path of the settings file, or null if the solution root, the <c>.vs</c> directory, or the settings file itself don't exist.</returns>
        private static string? FindSolutionSettingsFile(SVsServiceProvider serviceProvider)
        {
            var solutionRoot = SolutionHelper.GetSolutionDir(serviceProvider);
            if (solutionRoot == null)
                return null;

            var directories = Directory.GetDirectories(solutionRoot);
            if (!directories.Contains($"{solutionRoot}\\.vs"))
                return null;

            var solutionSettingsFile = Directory
                .EnumerateFiles($"{solutionRoot}\\.vs", SolutionSettingsFileName, SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            return solutionSettingsFile != null && File.Exists(solutionSettingsFile) ? solutionSettingsFile : null;
        }

        public class SolutionSettingsFile
        {
            public bool? ShouldNotifyUserToCreateCodigaConfig { get; set; } = true;
        }
    }
}