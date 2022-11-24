using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Extension.Rosie;
using Task = System.Threading.Tasks.Task;
using Extension.SnippetSearch;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio;
using System.Threading.Tasks;

namespace Extension
{
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideToolWindow(typeof(SnippetSearch.SearchWindow), Orientation = ToolWindowOrientation.Right, Style = VsDockStyle.Tabbed)]
	[Guid(ExtensionPackage.PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideOptionPage(typeof(Settings.CodigaOptionPage), "Codiga", "General", 0, 0, true, SupportsProfiles = true)]
	//See https://github.com/madskristensen/SolutionLoadSample
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class ExtensionPackage : AsyncPackage
	{
		/// <summary>
		/// SnippetSearchPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "e8d2d8f8-96dc-4c92-bb81-346b4d2318e4";

		private CancellationToken _cancellationToken;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExtensionPackage"/> class.
		/// </summary>
		public ExtensionPackage()
		{
		}
		
		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			_cancellationToken = cancellationToken;
            var isSolutionLoaded = await IsSolutionLoadedAsync();

            if (isSolutionLoaded)
	            InitializeRulesCache();

            //Inits the cache only after a solution is loaded completely 
            SolutionEvents.OnAfterBackgroundSolutionLoadComplete += InitializeRulesCache;
            SolutionEvents.OnAfterCloseSolution += DisposeRulesCache;

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
			await SnippetSearchMenuCommand.InitializeAsync(this);
        }

		//See https://github.com/madskristensen/SolutionLoadSample
        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var vsSolution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            ErrorHandler.ThrowOnFailure(vsSolution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));

            return value is bool isSolutionOpen && isSolutionOpen;
        }
        
        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			if (toolWindowType == typeof(SnippetSearch.SearchWindow).GUID)
			{
				return this;
			}

			return base.GetAsyncToolWindowFactory(toolWindowType);
		}

		protected override string GetToolWindowTitle(Type toolWindowType, int id)
		{
			if (toolWindowType == typeof(SnippetSearch.SearchWindow))
			{
				return "SnippetSearch loading";
			}

			return base.GetToolWindowTitle(toolWindowType, id);
		}

		private async void InitializeRulesCache(object sender = null, EventArgs e = null)
		{
			//Switching back to main thread due to RosieRulesCache.StartPolling()
			await JoinableTaskFactory.SwitchToMainThreadAsync(_cancellationToken);
			RosieRulesCache.Initialize();
		}

		private static void DisposeRulesCache(object sender, EventArgs e)
		{
            RosieRulesCache.Dispose();
        }

		#endregion
	}
}
