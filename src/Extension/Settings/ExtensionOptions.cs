using Community.VisualStudio.Toolkit;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Extension.Settings
{
	internal partial class OptionsPageProvider
	{
		// Register the options with this attribute on your package class:
		[ComVisible(true)]
		public class ExtensionOptionsPage : BaseOptionPage<CodigaOptions> { }
	}

	public class CodigaOptions : BaseOptionModel<CodigaOptions>
	{
		[Category("Codiga")]
		[DisplayName("Use coding assistant")]
		[Description("Enables or Disables the Codiga shortcut search triggered by \".\"")]
		[DefaultValue(true)]
		public bool UseCodingAssistant { get; set; } = true;

		[Category("Codiga")]
		[DisplayName("Use inline completion")]
		[Description("Enables or Disables the Codiga inline snippet search triggered by inline comments")]
		[DefaultValue(true)]
		public bool UseInlineCompletion { get; set; } = true;

		[Category("Codiga")]
		[DisplayName("API token")]
		[Description("The codiga API token.")]
		[DefaultValue(true)]
		public string ApiToken { get; set; }
	}
}
