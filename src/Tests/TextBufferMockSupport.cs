using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Moq;

namespace Tests
{
    public static class TextBufferMockSupport
    {
        /// <summary>
        /// Creates a mock <c>ITextBuffer</c> with the provided <c>bufferText</c> as its content without mocking
        /// the individual lines in the buffer.
        /// <br/>
        /// Any result of a violation fix edit will be saved into <c>bufferContent</c>.
        /// </summary>
        public static Mock<ITextBuffer> MockTextBuffer(string bufferText, out TextBufferContent bufferContent)
        {
            bufferContent = new TextBufferContent(bufferText);
            var textSnapshot = MockTextSnapshot(out var textBuffer, bufferContent);
            MockLineStartPosition(textSnapshot, 0, 0);
            return textBuffer;
        }

        /// <summary>
        /// Mocks an <c>ITextBuffer</c> with the buffer content's length (or an arbitrary one if no content provided),
        /// as the document being tagged, and an <c>ITextSnapshot</c> that is returned by <c>ITextBuffer.CurrentSnapshot</c>.
        /// <br/>
        /// Optionally, depending on the test case, it mocks or not, the individual lines in the buffer.
        /// </summary>
        public static Mock<ITextSnapshot> MockTextSnapshot(out Mock<ITextBuffer> buffer, TextBufferContent? bufferContent = null, bool mockLines = false)
        {
            var textSnapshot = new Mock<ITextSnapshot>();
            var textBuffer = new Mock<ITextBuffer>();
            textSnapshot.Setup(tss => tss.Length).Returns(bufferContent != null ? bufferContent.Text.Length : 50);
            textSnapshot.Setup(tss => tss.TextBuffer).Returns(textBuffer.Object);
            textBuffer.Setup(b => b.CurrentSnapshot).Returns(textSnapshot.Object);
            buffer = textBuffer;
            
            if (mockLines)
                MockLines(bufferContent, textSnapshot);

            //Mock modification interactions within the text buffer
            MockInsertionInTextBuffer(textBuffer, bufferContent);
            MockDeletionInTextBuffer(textBuffer, bufferContent);
            MockReplacementInTextBuffer(textBuffer, bufferContent);
            
            return textSnapshot;
        }

        /// <summary>
        /// Mocks all line's start and end positions in the buffer content, based on the positions of \n characters in the buffer text
        /// </summary>
        private static void MockLines(TextBufferContent bufferContent, Mock<ITextSnapshot> textSnapshot)
        {
            var indexes = new List<int>();
            // for loop ends when i=-1 ('\n' not found)
            for (var i = bufferContent.Text.IndexOf("\n"); i > -1; i = bufferContent.Text.IndexOf("\n", i + 1))
                indexes.Add(i);

            var lines = bufferContent.Text.Split('\n');
            //First line always starts at 0
            MockLine(textSnapshot, 0, 0, lines[0].Length + 1, bufferContent);
            for (var i = 0; i <= indexes.Count - 1; i++)
            {
                MockLine(textSnapshot, 
                    i + 1,
                    indexes[i] + 1,
                    //Line ends after the next \n character, or at the text buffer's end
                    i < indexes.Count - 1 ? indexes[i + 1] + 1 : bufferContent.Text.Length,
                    bufferContent);
            }
        }

        /// <summary>
        /// Mocks a single line for <c>RosiePosition.GetOffset()</c>, meaning a line with the given <c>lineNumber</c>
        /// starts at the provided <c>startPosition</c>.
        /// <br/>
        /// Also mocks the same line and its content to be returned for a range of start and end positions,
        /// that enclose that given line.
        /// <br/>
        /// The end position value should take into account and include the \r\n characters at the end of each line. 
        /// </summary>
        private static void MockLine(
            Mock<ITextSnapshot> textSnapshot,
            int lineNumber,
            int start,
            int end,
            TextBufferContent bufferContent)
        {
            var snapshotLine = new Mock<ITextSnapshotLine>();
            textSnapshot.Setup(tss => tss.GetLineFromLineNumber(lineNumber))
                .Returns(snapshotLine.Object);
            textSnapshot.Setup(tss => tss.GetLineFromPosition(It.IsInRange(start, end, Range.Inclusive)))
                .Returns(snapshotLine.Object);

            var lines = bufferContent.Text.Replace("\r", "").Split('\n');
            snapshotLine.Setup(l => l.Start)
                .Returns(new SnapshotPoint(textSnapshot.Object, start));
            snapshotLine.Setup(l => l.End)
                .Returns(new SnapshotPoint(textSnapshot.Object, end));
            snapshotLine.Setup(l => l.LineNumber)
                .Returns(lineNumber);
            snapshotLine.Setup(l => l.GetText())
                .Returns(lines[lineNumber]);
        }
        
        /// <summary>
        /// Mocks a single line for <c>RosiePosition.GetOffset()</c>, meaning a line with the given <c>lineNumber</c>
        /// starts at the provided <c>startPosition</c>.
        /// </summary>
        public static void MockLineStartPosition(Mock<ITextSnapshot> textSnapshot, int lineNumber, int start)
        {
            var snapshotLine = new Mock<ITextSnapshotLine>();
            textSnapshot.Setup(tss => tss.GetLineFromLineNumber(lineNumber))
                .Returns(snapshotLine.Object);
            snapshotLine.Setup(l => l.Start)
                .Returns(new SnapshotPoint(textSnapshot.Object, start));
        }

        /// <summary>
        /// Mocks the "add" edit type. It delegates the text insertion to a string object, and performs the edit
        /// on <c>bufferContent</c>.
        /// </summary>
        private static void MockInsertionInTextBuffer(Mock<ITextBuffer> textBuffer, TextBufferContent bufferContent)
        {
            textBuffer
                .Setup(tb => tb.Insert(It.IsAny<int>(), It.IsAny<string>()))
                .Callback(new InvocationAction(invocation =>
                    {
                        bufferContent.Text = bufferContent.Text.Insert((int)invocation.Arguments[0],
                            invocation.Arguments[1] as string);
                    }
                ));
        }

        /// <summary>
        /// Mocks the "remove" edit type. It delegates the text insertion to a string object, and performs the edit
        /// on <c>bufferContent</c>.
        /// </summary>
        private static void MockDeletionInTextBuffer(Mock<ITextBuffer> textBuffer, TextBufferContent bufferContent)
        {
            textBuffer
                .Setup(tb => tb.Delete(It.IsAny<Span>()))
                .Callback(new InvocationAction(invocation =>
                    {
                        var span = (Span)invocation.Arguments[0];
                        bufferContent.Text = bufferContent.Text.Remove(span.Start, span.Length);
                    }
                ));
        }

        /// <summary>
        /// Mocks the "update" edit type. It delegates the text insertion to a string object, and performs the edit
        /// on <c>bufferContent</c>.
        /// <br/>
        /// The update operation is emulated by a string <c>Remove()</c> and <c>Insert()</c> call because string has
        /// no counterpart of <c>ITextBuffer.Update()</c>.
        /// </summary>
        private static void MockReplacementInTextBuffer(Mock<ITextBuffer> textBuffer, TextBufferContent bufferContent)
        {
            textBuffer
                .Setup(tb => tb.Replace(It.IsAny<Span>(), It.IsAny<string>()))
                .Callback(new InvocationAction(invocation =>
                    {
                        var span = (Span)invocation.Arguments[0];
                        var updated = bufferContent.Text.Remove(span.Start, span.Length);
                        bufferContent.Text = updated.Insert(span.Start, invocation.Arguments[1] as string);
                    }
                ));
        }

        /// <summary>
        /// A wrapper class to store the content of an <c>ITextBuffer</c> and the results of any violation fix edits.
        /// </summary>
        public class TextBufferContent
        {
            public string Text { get; set; }

            public TextBufferContent(string text)
            {
                Text = text;
            }
        }
    }
}