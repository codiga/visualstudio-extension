using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Extension.Rosie.Annotation
{
    /// <summary>
    /// Provides tagging information for an <see cref="ITextBuffer"/>, so that a span of text can be
    /// marked/tagged with squiggles for an already available Rosie violation and <see cref="RosieViolationTag"/>.
    /// <br/>
    /// In order for the coloring and visual formatting to take effect, both an <see cref="EditorFormatDefinition"/>
    /// and an <see cref="ErrorTypeDefinition"/> instance must be exported for the same name for each color.
    /// There are four pairs of such classes defined here for the four severity levels available in Rosie.
    /// <br/>
    /// Instances of this class are created by <see cref="RosieViolationSquiggleTaggerProvider"/>.
    /// </summary>
    internal class RosieViolationSquiggleTagger : ITagger<IErrorTag>, IDisposable
    {
        #region Severity based squiggle color and format definitions

        private const string RosieViolationCritical = "Rosie Violation Critical";
        private const string RosieViolationError = "Rosie Violation Error";
        private const string RosieViolationWarning = "Rosie Violation Warning";
        private const string RosieViolationInfo = "Rosie Violation Informational";

        /// <summary>
        /// Defines the squiggle color for violations with 'critical' Rosie severity.
        /// </summary>
        [Export(typeof(EditorFormatDefinition))]
        [Name(RosieViolationCritical)]
        [UserVisible(true)]
        internal class RosieViolationCriticalFormatDefinition : EditorFormatDefinition
        {
            public RosieViolationCriticalFormatDefinition()
            {
                ForegroundColor = Colors.Red;
                BackgroundCustomizable = false;
                DisplayName = RosieViolationCritical;
            }
        }

        /// <summary>
        /// Defines the squiggle color for violations with 'error' Rosie severity.
        /// </summary>
        [Export(typeof(EditorFormatDefinition))]
        [Name(RosieViolationError)]
        [UserVisible(true)]
        internal class RosieViolationErrorFormatDefinition : EditorFormatDefinition
        {
            public RosieViolationErrorFormatDefinition()
            {
                ForegroundColor = Colors.Magenta;
                BackgroundCustomizable = false;
                DisplayName = RosieViolationError;
            }
        }

        /// <summary>
        /// Defines the squiggle color for violations with 'warning' Rosie severity.
        /// </summary>
        [Export(typeof(EditorFormatDefinition))]
        [Name(RosieViolationWarning)]
        [UserVisible(true)]
        internal class RosieViolationWarningFormatDefinition : EditorFormatDefinition
        {
            public RosieViolationWarningFormatDefinition()
            {
                ForegroundColor = Colors.Orange;
                BackgroundCustomizable = false;
                DisplayName = RosieViolationWarning;
            }
        }

        /// <summary>
        /// Defines the squiggle color for violations with 'informational' and 'unknown' Rosie severities.
        /// </summary>
        [Export(typeof(EditorFormatDefinition))]
        [Name(RosieViolationInfo)]
        [UserVisible(true)]
        internal class RosieViolationInfoFormatDefinition : EditorFormatDefinition
        {
            public RosieViolationInfoFormatDefinition()
            {
                ForegroundColor = Colors.White;
                BackgroundCustomizable = false;
                DisplayName = RosieViolationInfo;
            }
        }

        [Export(typeof(ErrorTypeDefinition))] [Name(RosieViolationCritical)]
        private readonly ErrorTypeDefinition RosieViolationCriticalTypeDefinition = null;

        [Export(typeof(ErrorTypeDefinition))] [Name(RosieViolationError)]
        private readonly ErrorTypeDefinition RosieViolationErrorTypeDefinition = null;

        [Export(typeof(ErrorTypeDefinition))] [Name(RosieViolationWarning)]
        private readonly ErrorTypeDefinition RosieViolationWarningTypeDefinition = null;

        [Export(typeof(ErrorTypeDefinition))] [Name(RosieViolationInfo)]
        private readonly ErrorTypeDefinition RosieViolationInformatioalTypeDefinition = null;

        #endregion

        /// <summary>
        /// Called when there was a change in the edited document, to signal that tagging must be updated.
        /// <br/>
        /// Instantiated automatically by the VS platform.
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <summary>
        /// It aggregates all <see cref="RosieViolationTag"/>s in the inspected text buffer.
        /// <br/>
        /// Based on the returned tags, this tagger can create the <see cref="IErrorTag"/>s,
        /// so that the colored squiggles can be applied in the text view.
        /// </summary>
        private readonly ITagAggregator<RosieViolationTag> _rosieViolationAggregator;

        /// <summary>
        /// The text buffer in which the squiggles are displayed.
        /// </summary>
        private readonly ITextBuffer _sourceBuffer;

        private bool _isDisposed;

        public RosieViolationSquiggleTagger(ITextBuffer sourceBuffer,
            ITagAggregator<RosieViolationTag> rosieViolationAggregator)
        {
            _sourceBuffer = sourceBuffer;
            _rosieViolationAggregator = rosieViolationAggregator;
            _rosieViolationAggregator.BatchedTagsChanged += OnTagsChanged;
        }

        /// <summary>
        /// Tt signals this tagger, thus all associated <see cref="ITagAggregator{T}"/>s to update the tags.
        /// </summary>
        /// <param name="sender">the sender of the event</param>
        /// <param name="e">the event arguments</param>
        private void OnTagsChanged(object sender, EventArgs e)
        {
            if (!_isDisposed)
                TagsChanged?.Invoke(this,
                    new SnapshotSpanEventArgs(new SnapshotSpan(_sourceBuffer.CurrentSnapshot,
                        new Span(0, _sourceBuffer.CurrentSnapshot.Length - 1))));
        }

        /// <summary>
        /// Creates new <see cref="RosieViolationSquiggleTag"/>s for each <see cref="RosieViolationTag"/>
        /// whose spans intersect with the provided span collection.
        /// <br/>
        /// The tag is created with the appropriate severity and message from the retrieved <see cref="RosieAnnotation"/>.
        /// </summary>
        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || _isDisposed)
                yield break;

            var snapshot = spans[0].Snapshot;
            foreach (var violationTagSpan in _rosieViolationAggregator.GetTags(spans))
            {
                var spanCollection = violationTagSpan.Span.GetSpans(snapshot);
                if (spanCollection.Count == 1)
                {
                    var errorSpan = spanCollection[0];
                    yield return new TagSpan<IErrorTag>(
                        errorSpan,
                        new RosieViolationSquiggleTag(
                            GetSquiggleTypeForRosieSeverity(violationTagSpan.Tag.Annotation.Severity),
                            violationTagSpan.Tag.Annotation.Message));
                }
            }
        }

        /// <summary>
        /// Maps the provided Rosie severity (see <see cref="RosieSeverities"/>) to its respective
        /// editor format definition name.
        /// <br/>
        /// For informational and unknown severities, it returns <see cref="RosieViolationInfo"/>.
        /// </summary>
        /// <param name="severity">The Rosie severity</param>
        /// <returns>The respective format definition name.</returns>
        private static string GetSquiggleTypeForRosieSeverity(string severity)
        {
            if (StringUtils.AreEqualIgnoreCase(RosieSeverities.Critical, severity))
                return RosieViolationCritical;
            if (StringUtils.AreEqualIgnoreCase(RosieSeverities.Error, severity))
                return RosieViolationError;
            if (StringUtils.AreEqualIgnoreCase(RosieSeverities.Warning, severity))
                return RosieViolationWarning;

            return RosieViolationInfo;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _rosieViolationAggregator.BatchedTagsChanged -= OnTagsChanged;
                _rosieViolationAggregator.Dispose();
            }
        }
    }
}