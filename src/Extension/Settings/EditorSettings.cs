using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension
{
	public class EditorSettings
	{
		public string FontFamily { get; }
		public short FontSize { get; }
		public int TabSize { get; }
		public bool UseSpace { get; }
		public int IndentSize { get; }

		internal EditorSettings(short fontSize, string fontFamily, int tabSize, bool useSpace, int indentSize)
		{
			FontSize = fontSize;
			FontFamily = fontFamily;
			TabSize = tabSize;
			UseSpace = useSpace;
			IndentSize = indentSize;
		}
	}

	public static class EditorSettingsProvider
	{
		public static EditorSettings GetCurrentEditorSettings(DTE dte, IWpfTextView wpfTextView)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			});

			var propertiesList = dte.get_Properties("FontsAndColors", "TextEditor");
			var fontSize = (short)propertiesList.Item("FontSize").Value;
			var fontFamily = (string)propertiesList.Item("FontFamily").Value;

			var tabSize = wpfTextView.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
			var useSpace = wpfTextView.Options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);
			var indentSize = wpfTextView.Options.GetOptionValue(DefaultOptions.IndentSizeOptionId);

			return new EditorSettings(fontSize, fontFamily, tabSize, useSpace, indentSize);
		}
	}
}
