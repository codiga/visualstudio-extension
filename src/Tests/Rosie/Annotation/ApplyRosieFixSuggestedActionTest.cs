using System;
using System.Collections.Generic;
using System.Threading;
using Extension.Rosie.Annotation;
using Extension.Rosie.Model;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using static Tests.TextBufferMockSupport;

namespace Tests.Rosie.Annotation
{
    /// <summary>
    /// Unit test for <see cref="ApplyRosieFixSuggestedAction"/>.
    /// </summary>
    [TestFixture]
    public class ApplyRosieFixSuggestedActionTest
    {
        #region Display text

        [Test]
        public void DisplayText_should_return_display_text()
        {
            var fix = new RosieViolationFix { Description = "Apply this change" };
            var applyFix = new ApplyRosieFixSuggestedAction(new Mock<ITextBuffer>().Object, fix);

            Assert.That(applyFix.DisplayText, Is.EqualTo("Fix: Apply this change"));
        }

        #endregion

        #region HasInvalidEditOffset

        [Test]
        public void HasInvalidEditOffset_should_not_apply_add_fix_when_edit_start_position_is_null()
        {
            var textBuffer = MockTextBuffer("some content", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit>
                {
                    new RosieViolationFixEdit { EditType = "add", Start = null, },
                    new RosieViolationFixEdit { EditType = "add", Start = new RosiePosition { Line = 1, Col = 0 } }
                }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("some content"));
        }

        [Test]
        public void HasInvalidEditOffset_should_not_apply_add_fix_when_edit_start_is_less_than_zero()
        {
            var textBuffer = MockTextBuffer("some content", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit>
                {
                    new RosieViolationFixEdit { EditType = "add", Start = new RosiePosition { Line = 1, Col = -10 } },
                    new RosieViolationFixEdit { EditType = "add", Start = new RosiePosition { Line = 1, Col = 0 } }
                }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("some content"));
        }

        [Test]
        public void HasInvalidEditOffset_should_not_apply_add_fix_when_edit_start_is_greater_than_document_end()
        {
            var textBuffer = MockTextBuffer("some content", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit>
                {
                    new RosieViolationFixEdit { EditType = "add", Start = new RosiePosition { Line = 1, Col = 60 } },
                    new RosieViolationFixEdit { EditType = "add", Start = new RosiePosition { Line = 1, Col = 0 } }
                }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("some content"));
        }

        [Test]
        public void HasInvalidEditOffset_should_not_apply_fix_when_edit_start_is_null()
        {
            var textBuffer = MockTextBuffer("content-to-remove", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit>
                {
                    new RosieViolationFixEdit
                    {
                        EditType = "remove",
                        Start = null,
                        End = new RosiePosition { Line = 1, Col = 7 }
                    }
                }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("content-to-remove"));
        }

        [Test]
        public void HasInvalidEditOffset_should_not_apply_fix_when_edit_end_is_null()
        {
            var textBuffer = MockTextBuffer("content-to-remove", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit>
                {
                    CreateEdit("remove", 1, 0, 1, 7),
                    new RosieViolationFixEdit
                    {
                        EditType = "remove",
                        Start = new RosiePosition { Line = 1, Col = 0 },
                        End = null
                    }
                }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("content-to-remove"));
        }

        [Test]
        public void HasInvalidEditOffset_should_not_apply_fix_when_edit_start_is_less_than_zero()
        {
            var textBuffer = MockTextBuffer("content-to-remove", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit> { CreateEdit("remove", 1, -10, 1, 7) }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("content-to-remove"));
        }

        [Test]
        public void HasInvalidEditOffset_should_not_apply_fix_when_edit_end_is_less_than_zero()
        {
            var textBuffer = MockTextBuffer("content-to-remove", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit> { CreateEdit("remove", 1, 0, 1, -10) }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("content-to-remove"));
        }

        [Test]
        public void HasInvalidEditOffset_should_not_apply_fix_when_edit_start_is_greater_than_document_end()
        {
            var textBuffer = MockTextBuffer("content-to-remove", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit> { CreateEdit("remove", 1, 40, 1, 7) }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("content-to-remove"));
        }

        [Test]
        public void HasInvalidEditOffset_should_not_apply_fix_when_edit_end_is_greater_than_document_end()
        {
            var textBuffer = MockTextBuffer("content-to-remove", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit> { CreateEdit("remove", 1, 0, 1, 40) }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("content-to-remove"));
        }

        [Test]
        public void HasInvalidEditOffset_should_not_apply_fix_when_line_is_out_of_range()
        {
            var bufferContent = new TextBufferContent("some content");
            var textSnapshot = MockTextSnapshot(out var textBuffer, bufferContent);
            textSnapshot
                .Setup(tss => tss.GetLineFromLineNumber(It.IsAny<int>()))
                .Throws<IndexOutOfRangeException>();

            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit> { CreateEdit("add", 1, 5, 1, 10) }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("some content"));
        }

        #endregion

        #region Invoke

        [Test]
        public void Invoke_should_not_apply_fix_when_there_is_no_fix_edit_available()
        {
            var textBuffer = MockTextBuffer("content-to-remove", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit>()
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("content-to-remove"));
        }
        
        [Test]
        public void Invoke_should_not_apply_edits_with_unknown_edit_types()
        {
            var textBuffer = MockTextBuffer("content-to-modify", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit>
                {
                    CreateEdit("remove", 1, 0, 1, 10),
                    CreateEdit("unknown", 1, 0, 1, 10, "inserted")
                }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("o-modify"));
        }

        [Test]
        public void Invoke_should_insert_text()
        {
            var textBuffer = MockTextBuffer("content-to-insert_into", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit> { CreateEdit("add", 1, 5, 1, 10, "added_text") }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("contadded_textent-to-insert_into"));
        }

        [Test]
        public void Invoke_should_update_text()
        {
            var textBuffer = MockTextBuffer("content-to-update", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit> { CreateEdit("update", 1, 0, 1, 10, "replacement_text") }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("replacement_texto-update"));
        }

        [Test]
        public void Invoke_should_remove_text()
        {
            var textBuffer = MockTextBuffer("content-to-remove", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit> { CreateEdit("remove", 1, 0, 1, 10) }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("o-remove"));
        }

        [Test]
        public void Invoke_should_apply_multiple_edits()
        {
            var textBuffer = MockTextBuffer("content-to-modify", out var bufferContent);
            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit>
                {
                    CreateEdit("remove", 1, 0, 1, 10),
                    CreateEdit("add", 1, 0, 1, 10, "inserted")
                }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo("insertedo-modify"));
        }

        [Test]
        public void Invoke_should_apply_update_text_in_multiple_lines_with_single_line_replacement()
        {
            var bufferContent = new TextBufferContent(@"first-row
second-row
third-row");
            MockTextSnapshot(out var textBuffer, bufferContent, true);

            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit> { CreateEdit("update", 2, 6, 3, 3, "replacement") }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo(@"first-row
seconreplacementird-row"));
        }

        [Test]
        public void Invoke_should_apply_update_text_in_multiple_lines_with_multi_line_replacement()
        {
            var bufferContent = new TextBufferContent(@"first-row
second-row
third-row");
            MockTextSnapshot(out var textBuffer, bufferContent, true);

            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit>
                {
                    CreateEdit("update", 2, 6, 3, 3, @"replacement
text")
                }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo(@"first-row
seconreplacement
textird-row"));
        }

        [Test]
        public void Invoke_should_apply_remove_text_in_multiple_lines()
        {
            var bufferContent = new TextBufferContent(@"first-row
second-row
third-row");
            MockTextSnapshot(out var textBuffer, bufferContent, true);

            var fix = new RosieViolationFix
            {
                Description = "Apply this change",
                Edits = new List<RosieViolationFixEdit> { CreateEdit("remove", 2, 6, 3, 3) }
            };

            new ApplyRosieFixSuggestedAction(textBuffer.Object, fix).Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo(@"first-row
seconird-row"));
        }

        #endregion

        #region Mock and test object creation

        /// <summary>
        /// Creates a <c>RosieViolationFixEdit</c> with the provided data.
        /// </summary>
        private static RosieViolationFixEdit CreateEdit(string editType, int startLine, int startCol, int endLine,
            int endCol,
            string? content = null)
        {
            var edit = new RosieViolationFixEdit
            {
                EditType = editType,
                Start = new RosiePosition { Line = startLine, Col = startCol },
                End = new RosiePosition { Line = endLine, Col = endCol }
            };

            if (content != null)
                edit.Content = content;

            return edit;
        }

        #endregion
    }
}