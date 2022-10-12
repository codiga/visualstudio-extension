using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Text.Editor;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension
{
	internal static class EditorSettings
	{
		private static IEnumerable<EditorOptionDefinition> Options { get; }

		//public static int IndentSize => Options

		static EditorSettings()
		{
			//Options = VS.Documents.GetActiveDocumentViewAsync().Result.TextView.Options.;

		}
	}
}
