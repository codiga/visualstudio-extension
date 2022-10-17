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

			ForegroundOpacity = 0.5;
			IsItalic = true;
		}
	}
}
