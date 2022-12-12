using System.Threading;
using Extension.Rosie;
using Extension.Rosie.Annotation;
using Extension.Rosie.Model;
using Microsoft.VisualStudio.Text;
using Moq;
using NUnit.Framework;
using static Tests.TextBufferMockSupport;

namespace Tests.Rosie.Annotation
{
    /// <summary>
    /// Unit test for <see cref="DisableRosieAnalysisSuggestedAction"/>.
    /// </summary>
    [TestFixture]
    public class DisableRosieAnalysisSuggestedActionTest
    {
        [Test]
        public void DisplayText_should_return_display_text()
        {
            var action = new DisableRosieAnalysisSuggestedAction(
                CreateRosieAnnotation("This is the rule name", 0, 1, 2, 3),
                new Mock<ITextBuffer>().Object);

            Assert.That(action.DisplayText, Is.EqualTo("Remove error 'This is the rule name'"));
        }

        [Test]
        public void Invoke_should_add_comment_when_invoked_on_annotation_from_document_start()
        {
            var bufferContent = new TextBufferContent(@"first-row
second-row
third-row");
            MockTextSnapshot(out var textBuffer, bufferContent, true);

            var action = new DisableRosieAnalysisSuggestedAction(
                CreateRosieAnnotation("rulename", 1, 0, 1, 3),
                textBuffer.Object,
                new TextBufferDataProvider { FileName = _ => "python_file.py", IsTestMode = true });

            action.Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo(@"# codiga-disable
first-row
second-row
third-row"));
        }

        [Test]
        public void Invoke_should_add_comment_when_invoked_in_first_line()
        {
            var bufferContent = new TextBufferContent(@"first-row
second-row
third-row");
            MockTextSnapshot(out var textBuffer, bufferContent, true);

            var action = new DisableRosieAnalysisSuggestedAction(
                CreateRosieAnnotation("rulename", 1, 5, 1, 8),
                textBuffer.Object,
                new TextBufferDataProvider { FileName = _ => "python_file.py", IsTestMode = true });

            action.Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo(@"# codiga-disable
first-row
second-row
third-row"));
        }

        [Test]
        public void Invoke_should_add_comment_when_invoked_on_annotation_from_line_end()
        {
            var bufferContent = new TextBufferContent(@"first-row
second-row
third-row");
            MockTextSnapshot(out var textBuffer, bufferContent, true);

            var action = new DisableRosieAnalysisSuggestedAction(
                CreateRosieAnnotation("rulename", 1, 11, 2, 0),
                textBuffer.Object,
                new TextBufferDataProvider { FileName = _ => "python_file.py", IsTestMode = true });

            action.Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo(@"# codiga-disable
first-row
second-row
third-row"));
        }

        [Test]
        public void Invoke_should_add_comment_when_invoked_in_not_first_not_last_line()
        {
            var bufferContent = new TextBufferContent(@"first-row
  second-row
third-row");
            MockTextSnapshot(out var textBuffer, bufferContent, true);

            var action = new DisableRosieAnalysisSuggestedAction(
                CreateRosieAnnotation("rulename", 2, 0, 2, 5),
                textBuffer.Object,
                new TextBufferDataProvider { FileName = _ => "python_file.py", IsTestMode = true });

            action.Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo(@"first-row
  # codiga-disable
  second-row
third-row"));
        }

        [Test]
        public void Invoke_should_add_comment_when_invoked_in_last_line()
        {
            var bufferContent = new TextBufferContent(@"first-row
  second-row
    third-row");
            MockTextSnapshot(out var textBuffer, bufferContent, true);

            var action = new DisableRosieAnalysisSuggestedAction(
                CreateRosieAnnotation("rulename", 3, 6, 3, 9),
                textBuffer.Object,
                new TextBufferDataProvider { FileName = _ => "python_file.py", IsTestMode = true });

            action.Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo(@"first-row
  second-row
    # codiga-disable
    third-row"));
        }

        [Test]
        public void Invoke_should_add_comment_when_invoked_on_annotation_from_document_end()
        {
            var bufferContent = new TextBufferContent(@"first-row
  second-row
    third-row");
            MockTextSnapshot(out var textBuffer, bufferContent, true);

            var action = new DisableRosieAnalysisSuggestedAction(
                CreateRosieAnnotation("rulename", 3, 14, 3, 14),
                textBuffer.Object,
                new TextBufferDataProvider { FileName = _ => "python_file.py", IsTestMode = true });

            action.Invoke(new CancellationToken());

            Assert.That(bufferContent.Text, Is.EqualTo(@"first-row
  second-row
    # codiga-disable
    third-row"));
        }

        /// <summary>
        /// Creates a <c>RosieAnnotation</c> with the given rule name, and an underlying <c>RosieViolation</c>
        /// with the provided start and end positions.
        /// </summary>
        private static RosieAnnotation CreateRosieAnnotation(string ruleName, int startLine, int startCol, int endLine,
            int endCol)
        {
            return new RosieAnnotation(ruleName, "ruleset-name",
                new RosieViolation
                {
                    Start = new RosiePosition { Line = startLine, Col = startCol },
                    End = new RosiePosition { Line = endLine, Col = endCol }
                });
        }
    }
}