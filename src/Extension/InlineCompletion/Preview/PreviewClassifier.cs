using Extension.InlineCompletion.Preview;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Extension.InlineCompletion.Preview
{
	/// <summary>
	/// Classifier that classifies the preview snippet code.
	/// </summary>
	internal class PreviewClassifier : IClassifier
	{
		/// <summary>
		/// Classification type.
		/// </summary>
		private readonly IClassificationType _classificationType;

		/// <summary>
		/// Initializes a new instance of the <see cref="PreviewClassifier"/> class.
		/// </summary>
		/// <param name="registry">Classification registry.</param>
		internal PreviewClassifier(IClassificationTypeRegistryService registry)
		{
			_classificationType = registry.GetClassificationType("PreviewClassifier");
		}

		/// <summary>
		/// An event that occurs when the classification of a span of text has changed.
		/// </summary>
		/// <remarks>
		/// This event gets raised if a non-text change would affect the classification in some way,
		/// for example typing /* would cause the classification to change in C# without directly
		/// affecting the span.
		/// </remarks>
		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;


		/// <summary>
		/// Returns new <see cref="ClassificationSpan"/>s based on <see cref="CodePreviewSession.CurrentPreview"/> of the completion client.
		/// </summary>
		/// <param name="span"></param>
		/// <returns></returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			var result = new List<ClassificationSpan>();

			if(CodePreviewSession.CurrentPreview != null && _classificationType != null)
			{
				result.Add(new ClassificationSpan(CodePreviewSession.CurrentPreview.Span.GetSpan(span.Snapshot), _classificationType));
			}	

			return result;
		}
	}

	internal static class PreviewClassificationDefinition
	{
		/// <summary>
		/// Defines the classification type so that it is added to the <see cref="IClassificationTypeRegistryService"/>.
		/// </summary>
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("PreviewClassifier")]
		private static ClassificationTypeDefinition s_typeDefinition;
	}
}
