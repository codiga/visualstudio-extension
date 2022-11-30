using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Extension.Rosie.Model;
using Microsoft.VisualStudio.Text;
using Span = Microsoft.VisualStudio.Text.Span;

namespace Extension.Rosie.Annotation
{
    /// <summary>
    /// Applies a fix with a series of edits on the code.
    /// </summary>
    public class ApplyRosieFixSuggestedAction : ISuggestedAction
    {
        private readonly ITextBuffer _textBuffer;
        private readonly IList<RosieViolationFixEdit> _edits;
        private readonly string _displayText;

        public ApplyRosieFixSuggestedAction(ITextBuffer textBuffer, RosieViolationFix fix)
        {
            _textBuffer = textBuffer;
            _edits = fix.Edits;
            _displayText = $"Fix: {fix.Description}";
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            if (HasInvalidEditOffset())
                return;

            foreach (var edit in _edits)
            {
                //Apply code insertion/addition
                if (StringUtils.AreEqualIgnoreCase(edit.EditType, RosieEditTypes.Add))
                {
                    _textBuffer.Insert(edit.Start.GetOffset(_textBuffer), edit.Content);
                }

                //Apply code replacement/update
                if (StringUtils.AreEqualIgnoreCase(edit.EditType, RosieEditTypes.Update))
                {
                    var replacementSpan =
                        Span.FromBounds(edit.Start.GetOffset(_textBuffer), edit.End.GetOffset(_textBuffer));
                    _textBuffer.Replace(replacementSpan, edit.Content);
                }

                //Apply code removal
                if (StringUtils.AreEqualIgnoreCase(edit.EditType, RosieEditTypes.Remove))
                {
                    var removalSpan =
                        Span.FromBounds(edit.Start.GetOffset(_textBuffer), edit.End.GetOffset(_textBuffer));
                    _textBuffer.Delete(removalSpan);
                }
            }
        }

        /// <summary>
        /// If the start offset for additions, or the start/end offset for removals and updates, received from the rule configuration,
        /// is either null or is outside the current file's range, we don't apply the fix.
        /// </summary>
        internal bool HasInvalidEditOffset()
        {
            var hasInvalidOffset = true;
            try
            {
                var documentLastPosition = _textBuffer.CurrentSnapshot.Length - 1;
                hasInvalidOffset = _edits
                    .Any(edit =>
                    {
                        int? startPosition;
                        if (StringUtils.AreEqualIgnoreCase(edit.EditType, RosieEditTypes.Add))
                        {
                            startPosition = edit.Start?.GetOffset(_textBuffer);
                            return startPosition == null || startPosition < 0 || startPosition > documentLastPosition;
                        }
                        
                        startPosition = edit.Start?.GetOffset(_textBuffer);
                        int? endPosition = edit.End?.GetOffset(_textBuffer);

                        return startPosition == null ||
                               endPosition == null ||
                               startPosition < 0 ||
                               endPosition < 0 ||
                               startPosition > documentLastPosition ||
                               endPosition > documentLastPosition;
                    });
            }
            catch (IndexOutOfRangeException)
            {
                //Let it through, no edit will happen.
            }

            return hasInvalidOffset;
        }

        #region Action sets and preview

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
        }

        #endregion

        #region Properties

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        public bool HasActionSets => false;

        public string DisplayText => _displayText;

        public ImageMoniker IconMoniker => default;

        public string IconAutomationText => null;

        public string InputGestureText => null;

        public bool HasPreview => false;

        #endregion
    }
}
