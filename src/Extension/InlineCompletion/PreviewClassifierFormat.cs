using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extension.InlineCompletion
{
	/// <summary>
	/// Defines an editor format for the EditorClassifier1 type that has a purple background
	/// and is underlined.
	/// </summary>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "PreviewClassifier")]
	[Name("PreviewClassifier")]
	[UserVisible(true)] // This should be visible to the end user
	[Order(After = Priority.High)]
	internal sealed class PreviewClassifierFormat : ClassificationFormatDefinition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PreviewClassifierFormat"/> class.
		/// </summary>
		public PreviewClassifierFormat()
		{
			// switch to main task to be able to access DTE
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			});

			var vssp = VS.GetMefService<SVsServiceProvider>();
			var dte = (_DTE)vssp.GetService(typeof(_DTE));

			var settings = EditorSettingsProvider.GetCurrentFontSettings(dte);

			ForegroundColor = settings.CommentColor;
			
		}
	}
}
