using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
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
using System.Windows.Media;

namespace Extension
{
	/// <summary>
	/// Represents the user settings for indentation
	/// </summary>
	public class IndentationSettings
	{
		public int TabSize { get; }
		public bool UseSpace { get; }
		public int IndentSize { get; }

		internal IndentationSettings(int indentSize, int tabSize, bool useSpace)
		{
			TabSize = tabSize;
			UseSpace = useSpace;
			IndentSize = indentSize;
		}
	}

	/// <summary>
	/// Represents the DTE based font settings for the text editor
	/// </summary>
	public class FontSettings
	{
		public string FontFamily { get; }
		public short FontSize { get; }
		public Color CommentColor { get; }
		public Color TextBackgroundColor { get; }

		internal FontSettings(short fontSize, string fontFamily, Color commentColor, Color textBackgroundColor)
		{
			FontSize = fontSize;
			FontFamily = fontFamily;
			CommentColor = commentColor;
			TextBackgroundColor = textBackgroundColor;
		}
	}

	public static class EditorSettingsProvider
	{
		/// <summary>
		/// Provides the current DTE-based font settings for the text editor
		/// </summary>
		/// <param name="dte"></param>
		/// <returns></returns>
		public static FontSettings GetCurrentFontSettings(DTE dte)
		{
			// switch to main task to be able to access DTE
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			});
			var propertiesList = dte.get_Properties("FontsAndColors", "TextEditor");

			var itemsList = (FontsAndColorsItems)propertiesList.Item("FontsAndColorsItems").Object;
			var commentItem = itemsList.Cast<ColorableItems>().Single(i => i.Name=="Comment");
			var colorBytes = BitConverter.GetBytes(commentItem.Foreground);
			var commentColor = Color.FromRgb(colorBytes[2], colorBytes[1], colorBytes[0]);

			var textItem = itemsList.Cast<ColorableItems>().Single(i => i.Name == "Plain Text");
			var bgColorBytes = BitConverter.GetBytes(textItem.Background);
			var bbgColor = Color.FromRgb(bgColorBytes[2], bgColorBytes[1], bgColorBytes[0]);
			var fontSize = (short)propertiesList.Item("FontSize").Value;
			var fontFamily = (string)propertiesList.Item("FontFamily").Value;

			return new FontSettings(fontSize, fontFamily, commentColor, bbgColor);
		}

		/// <summary>
		/// Provides the current indentaion settings based on the given editor view
		/// </summary>
		/// <param name="wpfTextView"></param>
		/// <returns></returns>
		public static IndentationSettings GetCurrentIndentationSettings()
		{
			IWpfTextView textView = null;
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				var docView = await VS.Documents.GetActiveDocumentViewAsync();
				textView = docView.TextView;
			});

			if(textView == null)
				return null;

			var tabSize = textView.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
			var useSpace = textView.Options.GetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId);
			var indentSize = textView.Options.GetOptionValue(DefaultOptions.IndentSizeOptionId);

			return new IndentationSettings(indentSize, tabSize, useSpace);
		}
	}
}
