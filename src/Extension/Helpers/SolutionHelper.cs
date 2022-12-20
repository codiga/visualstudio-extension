using System.IO;
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
        /// <param name="serviceProvider">The service provider to retrieve information about the solution from.</param>
        /// <returns></returns>
        internal static string? GetSolutionDir(SVsServiceProvider serviceProvider)
        {
            var sol = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            //'dir' contains the solution's directory path, or the open folder's path when it is a folder that's open, and not a solution
            sol.GetSolutionInfo(out string dir, out string file, out string ops);
            return Path.GetDirectoryName(dir);
        }
    }
}