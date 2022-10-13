using Community.VisualStudio.Toolkit;
using EnvDTE;
using Extension.AssistantCompletion;
using Extension.Caching;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Linq;

namespace Extension.InlineCompletion
{
	/// <summary>
	/// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
	/// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
	/// </summary>
	[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	internal sealed class WpfTextViewCreationListener : IWpfTextViewCreationListener
	{
		/// <summary>
		/// Defines the adornment layer for the adornment. This layer is ordered
		/// after the selection layer in the Z-order
		/// </summary>
		[Export(typeof(AdornmentLayerDefinition))]
		[Name("TextAdornment1")]
		[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
		private AdornmentLayerDefinition editorAdornmentLayer;

		[Import]
		internal IVsEditorAdaptersFactoryService AdapterService = null;

		[Import]
		internal ExpansionClient ExpansionClient;

		/// <summary>
		/// Called when a text view having matching roles is created over a text data model having a matching content type.
		/// Instantiates a TextAdornment1 manager when the textView is created.
		/// </summary>
		/// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
		public void TextViewCreated(IWpfTextView textView)
		{
			var vsTextView = AdapterService.GetViewAdapter(textView);
			var vssp = VS.GetMefService<SVsServiceProvider>();
			var dte = (_DTE)vssp.GetService(typeof(_DTE));

			var settings = EditorSettingsProvider.GetCurrentEditorSettings(dte, textView);

			new InlineCompletionClient(textView, vsTextView, ExpansionClient, settings);
			

			// The adornment will listen to any event that changes the layout (text changes, scrolling, etc)
		}

	}
}
