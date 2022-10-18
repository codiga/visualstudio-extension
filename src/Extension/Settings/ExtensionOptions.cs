using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace Extension.Settings
{
	[ComVisible(true)]
	[Guid("894acc3b-045e-47f7-8f36-b0c8a66e0083")]
	public class CodigaOptionPage : UIElementDialogPage
	{
		protected override UIElement Child
		{
			get
			{
				OptionsPage page = new OptionsPage
				{
					extensionOptionsPage = this
				};
				page.Initialize();
				return page;
			}
		}
	}

	internal partial class OptionsPageProvider
	{
		// Register the options with this attribute on your package class:
		[ComVisible(true)]
		public class ExtensionOptionsPage : BaseOptionPage<CodigaOptions> { }
	}

	public class CodigaOptions : BaseOptionModel<CodigaOptions>
	{
		[Category("Codiga")]
		//[DisplayName("Use coding assistant")]
		//[Description("Enables or Disables the Codiga shortcut search triggered by \".\"")]
		[DefaultValue(true)]
		public bool UseCodingAssistant { get; set; } = true;

		[Category("Codiga")]
		//[DisplayName("Use inline completion")]
		//[Description("Enables or Disables the Codiga inline snippet search triggered by inline comments")]
		[DefaultValue(true)]
		public bool UseInlineCompletion { get; set; } = true;

		[Category("Codiga")]
		//[DisplayName("API token")]
		//[Description("The codiga API token.")]
		[DefaultValue(true)]
		public string ApiToken { get; set; }
	}
}
