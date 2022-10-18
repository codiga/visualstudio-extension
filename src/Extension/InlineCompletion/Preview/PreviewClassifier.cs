using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Extension.InlineCompletion
{
	/// <summary>
	/// Classifier that classifies the preview snippet code.
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
		/// Returns new <see cref="ClassificationSpan"/>s based on <see cref="InlineCompletionClient.CurrentSnippetSpan"/> of the completion client.
		/// </summary>
		/// <param name="span"></param>
		/// <returns></returns>
		public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
		{
			var result = new List<ClassificationSpan>();

			//if(client.CurrentSnippetSpan != null && classificationType != null)
			//{
			//	result.Add(new ClassificationSpan(client.CurrentSnippetSpan.Span.GetSpan(span.Snapshot), classificationType));
			//}	

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
		private static ClassificationTypeDefinition typeDefinition;
	}
}
