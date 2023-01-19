using System.IO;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Extension.Helpers
{
    /// <summary>
    /// Utility to handle different aspects of a solution.
    /// </summary>
    internal static class SolutionHelper
    {
        /// <summary>
        /// Returns the solution's root dir, or in Open Folder mode, the open folder's path.
        /// </summary>
        /// <returns></returns>
        internal static async Task<string?> GetSolutionDir()
        {
            var serviceProvider = await GetServiceProvider();
            return serviceProvider == null ? null : GetSolutionDir(serviceProvider);
        }

        /// <summary>
        /// Returns the <see cref="SVsServiceProvider"/> that is used to retrieve the solution directory.
        /// </summary>
        /// <returns></returns>
        internal static async Task<SVsServiceProvider?> GetServiceProvider()
        {
            return await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return await VS.GetMefServiceAsync<SVsServiceProvider>();
            });
        }
        
        /// <summary>
        /// Returns the solution's root dir, or in Open Folder mode, the open folder's path.
        /// </summary>
        /// <param name="serviceProvider">The service provider to retrieve information about the solution from.</param>
        /// <returns></returns>
        internal static string? GetSolutionDir(SVsServiceProvider serviceProvider)
        {
            var sol = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            //'dir' contains the solution's directory path, or the open folder's path when it is a folder that's open, and not a solution
            sol.GetSolutionInfo(out string dir, out string file, out string ops);
            return Path.GetDirectoryName(dir);
        }

        /// <summary>
        /// Returns whether the argument solution is in Open Folder mode.
        /// </summary>
        /// <param name="solution">The solution service</param>
        internal static bool IsInOpenFolderMode(IVsSolution solution)
        {
            solution.GetProperty((int)__VSPROPID7.VSPROPID_IsInOpenFolderMode, out object folderMode);
            return folderMode is bool isInOpenFolderMode && isInOpenFolderMode;
        }
    }
}