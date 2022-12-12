using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extension.Rosie.Model;
using Extension.SnippetFormats;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Extension.Rosie.Annotation
{
    /// <summary>
    /// Adds the <c>codiga-disable</c> string as a comment above the line on which this action is invoked.
    /// <br/>
    /// This will make the Rosie service ignore that line during analysis.
    /// </summary>
    public class DisableRosieAnalysisSuggestedAction : ISuggestedAction
    {
        private const string CodigaDisable = "codiga-disable";
        private readonly RosieAnnotation _annotation;
        private readonly ITextBuffer _textBuffer;
        private readonly TextBufferDataProvider _dataProvider;
        private readonly string _displayText;

        public DisableRosieAnalysisSuggestedAction(RosieAnnotation annotation, ITextBuffer textBuffer, TextBufferDataProvider? dataProvider = null)
        {
            _annotation = annotation;
            _textBuffer = textBuffer;
            _dataProvider = dataProvider ?? new TextBufferDataProvider();
            _displayText = $"Remove error '{annotation.RuleName}'";
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            var violationStartPosition = _annotation.Start.GetOffset(_textBuffer);
            var lineAtViolationStart = _textBuffer.CurrentSnapshot.GetLineFromPosition(violationStartPosition);
            var lineText = lineAtViolationStart.GetText();

            //Calculate the indentation length by counting the whitespace characters at the beginning of the violation's line.
            var indentationLength = 0;
            while (char.IsWhiteSpace(lineText[indentationLength]))
                indentationLength++;

            //If the violation is in the first line of the document
            if (lineAtViolationStart.LineNumber == 0)
            {
                //Insert the first line's new line text at the beginning of the first line, to add a new line
                _textBuffer.Insert(lineAtViolationStart.Start.Position,
                    GetNewLineText(new SnapshotPoint(_textBuffer.CurrentSnapshot,
                        lineAtViolationStart.Start.Position)));
            }
            //If the violation is NOT in the first line
            else
            {
                //Find the previous line's end position
                var previousLineEndPosition =
                    _textBuffer.CurrentSnapshot
                        .GetLineFromLineNumber(lineAtViolationStart.LineNumber == 0 ? 0 : lineAtViolationStart.LineNumber - 1)
                        .End.Position;
                
                //Insert the previous line's new line text at the end of the previous line, to add a new line
                _textBuffer.Insert(previousLineEndPosition,
                    GetNewLineText(new SnapshotPoint(_textBuffer.CurrentSnapshot, previousLineEndPosition)));
            }
            
            //Get the comment sign for the current file
            var language = LanguageUtils.ParseFromFileName(_dataProvider.FileName(_textBuffer));
            var commentSign = LanguageUtils.GetCommentSign(language);

            //Insert the "codiga-disable" comment at the new line's start position.
            //It uses 'lineAtViolationStart" because after inserting the new line character, the original violation's line number becomes
            //the new empty line's number.
            _textBuffer.Insert(lineAtViolationStart.Start.Position,
                $"{string.Concat(Enumerable.Repeat(" ", indentationLength))}{commentSign} {CodigaDisable}");
        }

        /// <summary>
        /// Returns the new line text for the line at the provided point/position.
        /// <br/>
        /// This is based on https://blog.paranoidcoding.com/2014/09/02/vsix-insert-newline.html, it is a modified version of it,
        /// because we are adding a new line before the current one, and not after it, and we do the line number check
        /// in <c>Invoke()</c>.
        /// <br/>
        /// In test mode, returning a fixed new line value prevents the need to mock a couple of methods.
        /// </summary>
        /// <param name="point">the position whose line's new line text we want to use</param>
        private string GetNewLineText(SnapshotPoint point)
        {
            if (_dataProvider.IsTestMode)
                return "\r\n";
            
            var line = point.GetContainingLine();
            return line.LineBreakLength > 0 ? line.GetLineBreakText() : Environment.NewLine;
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