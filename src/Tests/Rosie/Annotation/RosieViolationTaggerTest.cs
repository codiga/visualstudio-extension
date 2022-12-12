using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    /// Unit test for <see cref="RosieViolationTagger"/>.
    /// </summary>
    [TestFixture]
    public class RosieViolationTaggerTest
    {
        [Test]
        public async Task GetTags_should_return_no_tag_for_no_span()
        {
            var tagger = new RosieViolationTagger();
            var spanCollection =
                new NormalizedSnapshotSpanCollection(new Mock<ITextSnapshot>().Object, new List<Span>());
        
            var tagSpans = await tagger.GetTagsAsync(spanCollection);
        
            Assert.That(tagSpans, Is.Empty);
        }
        
        [Test]
        public async Task GetTags_should_return_no_tag_when_the_tagger_is_already_disposed()
        {
            var tagger = new RosieViolationTagger();
            var spanCollection = TaggingMockSupport.CreateSpanCollection(new Mock<ITextSnapshot>(), 0, 0);
        
            tagger.Dispose();
        
            var tagSpans = await tagger.GetTagsAsync(spanCollection);
        
            Assert.That(tagSpans, Is.Empty);
        }
        
        [Test]
        public async Task GetTags_should_return_no_tag_when_the_text_buffer_is_not_associated_with_a_filename()
        {
            var tagger =
                new RosieViolationTagger(new TextBufferDataProvider { FileName = _ => null, IsTestMode = true });
            var spanCollection = TaggingMockSupport.CreateSpanCollection(new Mock<ITextSnapshot>(), 0, 0);
        
            var tagSpans = await tagger.GetTagsAsync(spanCollection);
        
            Assert.That(tagSpans, Is.Empty);
        }
        
        [Test]
        public async Task GetTags_should_return_no_tag_for_no_rosie_violation()
        {
            var tagger = new RosieViolationTagger(
                new TextBufferDataProvider
                    { FileName = _ => "python_file.py", IsTestMode = true });
            var spanCollection = TaggingMockSupport.CreateSpanCollection(new Mock<ITextSnapshot>(), 0, 0);
        
            var tagSpans = await tagger.GetTagsAsync(spanCollection);
        
            Assert.That(tagSpans, Is.Empty);
        }
        
        [Test]
        public async Task GetTags_should_filter_out_violations_out_of_document_range()
        {
            var textSnapshot = MockTextSnapshot(out var ignored);
        
            //Mock RosiePosition.GetOffset() for line 1
            MockLineStartPosition(textSnapshot, 0, 10);
            //Mock RosiePosition.GetOffset() to throw exception for line 2
            textSnapshot.Setup(tss => tss.GetLineFromLineNumber(1)).Throws<ArgumentOutOfRangeException>();
        
            //Setup a non-empty span for the argument of GetTags()
            var spanCollection = TaggingMockSupport.CreateSpanCollection(textSnapshot, 21, 2);
        
            //Create the RosieAnnotations for violations returned from the server
            var intersectingSpan = CreateRosieAnnotation("rule1", 1, 11, 1, 14);
            var outOfRange = CreateRosieAnnotation("rule2", 2, 20, 2, 22);
            
            var tagger = new RosieViolationTagger(new TextBufferDataProvider
                { FileName = _ => "python_file.py", IsTestMode = true });
            tagger.Annotations = new List<RosieAnnotation> { intersectingSpan, outOfRange };
        
            var tagSpans = (await tagger.GetTagsAsync(spanCollection)).ToList();
        
            Assert.That(tagSpans, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(tagSpans[0].Span.Start.Position, Is.EqualTo(21));
                Assert.That(tagSpans[0].Span.End.Position, Is.EqualTo(23));
                Assert.That(tagSpans[0].Tag.Annotation, Is.EqualTo(intersectingSpan));
            });
        }
        
        [Test]
        public async Task GetTags_should_filter_out_violations_not_intersecting_the_current_span()
        {
            var bufferContent = new TextBufferContent(@"contents
here and there");
            var textSnapshot = MockTextSnapshot(out var ignored, bufferContent, true);

            //Setup a non-empty span for the argument of GetTags()
            var spanCollection = TaggingMockSupport.CreateSpanCollection(textSnapshot, 10, 5);
        
            //Create the RosieAnnotations for violations returned from the server
            var beforeSpan = CreateRosieAnnotation("rule1", 1, 5, 1, 9);
            var intersectingSpan = CreateRosieAnnotation("rule2", 2, 1, 2, 4);
            var afterSpan = CreateRosieAnnotation("rule3", 2, 10, 2, 12);
            
            var tagger = new RosieViolationTagger(new TextBufferDataProvider
                { FileName = _ => "python_file.py", IsTestMode = true });
            tagger.Annotations = new List<RosieAnnotation> { beforeSpan, intersectingSpan, afterSpan };
        
            var tagSpans = (await tagger.GetTagsAsync(spanCollection)).ToList();
        
            Assert.That(tagSpans, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(tagSpans[0].Span.Start.Position, Is.EqualTo(10));
                Assert.That(tagSpans[0].Span.End.Position, Is.EqualTo(13));
                Assert.That(tagSpans[0].Tag.Annotation, Is.EqualTo(intersectingSpan));
            });
        }
        
        [Test]
        public async Task GetTags_should_filter_out_violations_not_intersecting_empty_span()
        {
            var bufferContent = new TextBufferContent(@"contents
here and there");
            var textSnapshot = MockTextSnapshot(out var ignored, bufferContent, true);

            //Setup an empty span for the argument of GetTags()
            var spanCollection = TaggingMockSupport.CreateSpanCollection(textSnapshot, 10, 0);
        
            //Create the RosieAnnotations for violations returned from the server
            var beforeSpan = CreateRosieAnnotation("rule1", 1, 5, 1, 9);
            var intersectingSpan = CreateRosieAnnotation("rule2", 2, 0, 2, 4);
            var afterSpan = CreateRosieAnnotation("rule3", 2, 10, 2, 12);
            
            var tagger = new RosieViolationTagger(new TextBufferDataProvider
                { FileName = _ => "python_file.py", IsTestMode = true });
            tagger.Annotations = new List<RosieAnnotation> { beforeSpan, intersectingSpan, afterSpan };
        
            var tagSpans = (await tagger.GetTagsAsync(spanCollection)).ToList();
        
            Assert.That(tagSpans, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(tagSpans[0].Span.Start.Position, Is.EqualTo(10));
                Assert.That(tagSpans[0].Span.End.Position, Is.EqualTo(10));
                Assert.That(tagSpans[0].Tag.Annotation, Is.EqualTo(intersectingSpan));
            });
        }
        
        // ReSharper disable InconsistentNaming
        [Test]
        public async Task GetTags_should_return_tags()
        {
            var bufferContent = new TextBufferContent(@"contents
here and there
and over");
            var textSnapshot = MockTextSnapshot(out var ignored, bufferContent, true);
        
            //Setup an empty span for the argument of GetTags()
            var spanCollection = TaggingMockSupport.CreateSpanCollection(textSnapshot, 10, 15); //offset: 10-25
        
            //Create the RosieAnnotations for violations returned from the server
            var start_lte_span_start_end_gte_span_end =
                CreateRosieAnnotation("rule1", 1, 5, 3, 9);
            var start_lte_span_start_end_lt_span_end =
                CreateRosieAnnotation("rule2", 1, 5, 2, 4);
            var start_gt_span_start_end_gte_span_end =
                CreateRosieAnnotation("rule3", 2, 3, 3, 2);
            var start_gt_span_start_end_lt_span_end =
                CreateRosieAnnotation("rule4", 2, 3, 2, 6);
            
            var tagger = new RosieViolationTagger(new TextBufferDataProvider
                { FileName = _ => "python_file.py", IsTestMode = true });
            tagger.Annotations = new List<RosieAnnotation>
            {
                start_lte_span_start_end_gte_span_end,
                start_lte_span_start_end_lt_span_end,
                start_gt_span_start_end_gte_span_end,
                start_gt_span_start_end_lt_span_end
            };
        
            var tagSpans = (await tagger.GetTagsAsync(spanCollection)).ToList();
        
            Assert.That(tagSpans, Has.Count.EqualTo(4));
            Assert.Multiple(() =>
            {
                Assert.That(tagSpans[0].Span.Start.Position, Is.EqualTo(10));
                Assert.That(tagSpans[0].Span.End.Position, Is.EqualTo(25));
                Assert.That(tagSpans[0].Tag.Annotation, Is.EqualTo(start_lte_span_start_end_gte_span_end));
        
                Assert.That(tagSpans[1].Span.Start.Position, Is.EqualTo(10));
                Assert.That(tagSpans[1].Span.End.Position, Is.EqualTo(13));
                Assert.That(tagSpans[1].Tag.Annotation, Is.EqualTo(start_lte_span_start_end_lt_span_end));
        
                Assert.That(tagSpans[2].Span.Start.Position, Is.EqualTo(12));
                Assert.That(tagSpans[2].Span.End.Position, Is.EqualTo(25));
                Assert.That(tagSpans[2].Tag.Annotation, Is.EqualTo(start_gt_span_start_end_gte_span_end));
        
                Assert.That(tagSpans[3].Span.Start.Position, Is.EqualTo(12));
                Assert.That(tagSpans[3].Span.End.Position, Is.EqualTo(15));
                Assert.That(tagSpans[3].Tag.Annotation, Is.EqualTo(start_gt_span_start_end_lt_span_end));
            });
        }
        
        #region Mock and test object creation
        
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

        #endregion
    }
}