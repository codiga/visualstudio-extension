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

namespace Extension.InlineCompletion.Preview
{
	/// <summary>
	/// Defines an editor format for the Classifier used for previewing snippet code.
	/// </summary>
	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "PreviewClassifier")]
	[Name("PreviewClassifier")]
	[UserVisible(true)]
	[Order(After = Priority.High)]
	internal sealed class PreviewClassifierFormat : ClassificationFormatDefinition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PreviewClassifierFormat"/> class.
		/// </summary>
		public PreviewClassifierFormat()
		{
			// set the style of the snippet code preview
			ForegroundOpacity = 0.5;
			IsItalic = true;
		}
	}
}
