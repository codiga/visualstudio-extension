using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Extension.InlineCompletion
{
	/// <summary>
	/// Classifier that classifies all text as an instance of the "EditorClassifier1" classification type.
	/// </summary>
	internal class PreviewClassifier : IClassifier
	{
		/// <summary>
		/// Classification type.
		/// </summary>
		private readonly IClassificationType classificationType;
		private readonly InlineCompletionClient client;

		/// <summary>
		/// Initializes a new instance of the <see cref="PreviewClassifier"/> class.
		/// </summary>
		/// <param name="registry">Classification registry.</param>
		internal PreviewClassifier(IClassificationTypeRegistryService registry, InlineCompletionClient client)
		{
			this.classificationType = registry.GetClassificationType("PreviewClassifier");
			this.client = client;
		}

#pragma warning disable 67

		/// <summary>
		/// An event that occurs when the classification of a span of text has changed.
		/// </summary>
		/// <remarks>
		/// This event gets raised if a non-text change would affect the classification in some way,
		/// for example typing /* would cause the classification to change in C# without directly
		/// affecting the span.
		/// </remarks>
		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

#pragma warning restore 67

		/// <summary>
		/// Gets all the <see cref="ClassificationSpan"/> objects that intersect with the given range of text.
		/// </summary>
		/// <remarks>
		/// This method scans the given SnapshotSpan for potential matches for this classification.
		/// In this instance, it classifies everything and returns each span as a new ClassificationSpan.
		/// </remarks>
		/// <param name="span">The span currently being classified.</param>
		/// <returns>A list of ClassificationSpans that represent spans identified to be of this classification.</returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			// return new classification spans based on the current spans of the inline completion
			var result = new List<ClassificationSpan>();

			if(client.CurrentSnippetSpan != null && classificationType != null)
			{
				result.Add(new ClassificationSpan(client.CurrentSnippetSpan.Span.GetSpan(span.Snapshot), classificationType));
			}	

			return result;
		}
	}

	internal static class EditorClassifier1ClassificationDefinition
	{
		/// <summary>
		/// Defines the "PreviewClassifier" classification type.
		/// </summary>
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("PreviewClassifier")]
		private static ClassificationTypeDefinition typeDefinition;
	}
}
