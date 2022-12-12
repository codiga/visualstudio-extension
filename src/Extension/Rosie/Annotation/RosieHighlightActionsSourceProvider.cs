using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Extension.Rosie.Annotation
{
    /// <summary>
    /// Provides the following lightbulb actions for Rosie violations:
    /// - apply fix
    /// - disable the analysis on a violation by adding the codiga-disable comment
    /// - view the related rule on Codiga Hub
    /// <br/>
    /// See documentation at https://learn.microsoft.com/en-us/visualstudio/extensibility/walkthrough-displaying-light-bulb-suggestions?view=vs-2022.
    /// </summary>
    [Export(typeof(ISuggestedActionsSourceProvider)), ContentType("any"), Name("Rosie Code Analysis Actions")]
    internal class RosieHighlightActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        [Import] private readonly IViewTagAggregatorFactoryService tagAggregatorFactory = null;

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            return textBuffer == null || textView == null
                                      || !textView.Roles.Contains(PredefinedTextViewRoles.Document)
                                      || !textView.Roles.Contains(PredefinedTextViewRoles.Editable)
                                      || !textView.Roles.Contains(PredefinedTextViewRoles.PrimaryDocument)
                ? null
                : new RosieHighlightActionsSource(
                    tagAggregatorFactory.CreateTagAggregator<RosieViolationTag>(textView));
        }
    }

    /// <summary>
    /// Provides the lightbulb actions (apply fix, disable analysis, open rule on Codiga Hub) for requested ranges
    /// in a document.
    /// </summary>
    internal class RosieHighlightActionsSource : ISuggestedActionsSource
    {
        /// <summary>
        /// Called to signal that the list of actions has to be updated.
        /// </summary>
        public event EventHandler<EventArgs>? SuggestedActionsChanged;

        /// <summary>
        /// Collects the <c>RosieViolationTags</c> in a given range of the <see cref="ITextView"/> it is created for.
        /// See its creation in the constructor of <see cref="RosieHighlightActionsSourceProvider"/>.
        /// </summary>
        private readonly ITagAggregator<RosieViolationTag> _violationTagAggregator;

        private bool _isDisposed;

        public RosieHighlightActionsSource(ITagAggregator<RosieViolationTag> tagAggregator)
        {
            _violationTagAggregator = tagAggregator;
            _violationTagAggregator.BatchedTagsChanged += OnTagsChanged;
        }

        /// <summary>
        /// Signals to update the suggested actions if the tags have changed.
        /// </summary>
        private void OnTagsChanged(object sender, EventArgs e)
        {
            if (!_isDisposed)
                SuggestedActionsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Lightbulb actions are available only when there is at least one <see cref="RosieViolationTag"/> present
        /// in the inspected range.
        /// </summary>
        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Run(
                () => !_isDisposed && _violationTagAggregator.GetTags(range).Any(),
                cancellationToken);
        }

        /// <summary>
        /// Collects the lightbulb actions for the given <c>range</c>, where range can empty too.
        /// <br/>
        /// The tags for a given range are retrieved via <c>_violationTagAggregator</c>.
        /// <br/>
        /// This method maps the available actions to their respective violations.
        /// In more technical terms, maps <see cref="ISuggestedAction"/>s to their respective <see cref="RosieViolationTag"/>s.
        /// <br/>
        /// The 'disable-codiga' and 'open on Codiga Hub' actions are always available, while fixes are available only when there
        /// is at least one fix present for a violation.
        /// </summary>
        public IEnumerable<SuggestedActionSet> GetSuggestedActions(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            CancellationToken cancellationToken)
        {
            if (_isDisposed)
                return Enumerable.Empty<SuggestedActionSet>();

            var actions = new List<ISuggestedAction>();
            foreach (var violation in _violationTagAggregator.GetTags(range))
            {
                var rosieAnnotation = violation.Tag.Annotation;
                foreach (var fix in rosieAnnotation.Fixes)
                    actions.Add(new ApplyRosieFixSuggestedAction(range.Snapshot.TextBuffer, fix));

                actions.Add(new DisableRosieAnalysisSuggestedAction(
                    rosieAnnotation,
                    range.Snapshot.TextBuffer));
                actions.Add(new OpenOnCodigaHubSuggestedAction(rosieAnnotation));
            }

            return actions.Count != 0
                ? new[] { new SuggestedActionSet(null, actions, priority: SuggestedActionSetPriority.Medium) }
                : Enumerable.Empty<SuggestedActionSet>();
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _violationTagAggregator.BatchedTagsChanged -= OnTagsChanged;
                _violationTagAggregator.Dispose();
                _isDisposed = true;
            }
        }
    }
}