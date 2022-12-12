using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;

namespace Tests
{
    /// <summary>
    /// Utility to create a mock <see cref="SVsServiceProvider"/>. 
    /// </summary>
    internal static class ServiceProviderMockSupport
    {
        //For delegating calls of Microsoft.VisualStudio.Shell.Interop.IVsSolution.GetSolutionInfo(out string, out string, out string)
        private delegate void GetSolutionInfo(
            out string pbstrSolutionDirectory,
            out string pbstrSolutionFile,
            out string pbstrUserOptsFile);

        /// <summary>
        /// Creates a mock <c>SVsServiceProvider</c> and delegates its <c>GetSolutionInfo()</c> method call
        /// to a dedicated delegate method. 
        /// </summary>
        /// <param name="solutionDir">The solution directory path to be returned by <c>GetSolutionInfo()</c></param>
        internal static SVsServiceProvider MockServiceProvider(string solutionDir)
        {
            var serviceProvider = new Mock<SVsServiceProvider>();
            var solution = new Mock<IVsSolution>();
            serviceProvider.Setup(sp => sp.GetService(typeof(SVsSolution))).Returns(solution.Object);

            string dir, file, ops;
            solution
                .Setup(p => p.GetSolutionInfo(out dir, out file, out ops))
                .Callback(new GetSolutionInfo((out string dir, out string file, out string ops) =>
                {
                    dir = solutionDir;
                    file = "";
                    ops = "";
                }))
                .Returns(1);
            return serviceProvider.Object;
        }
    }
}
