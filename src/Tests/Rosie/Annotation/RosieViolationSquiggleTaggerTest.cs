using System.Collections.Generic;
using System.Linq;
using Extension.Rosie;
using Extension.Rosie.Annotation;
using Extension.Rosie.Model;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using NUnit.Framework;

namespace Tests.Rosie.Annotation
{
    /// <summary>
    /// Unit test for <see cref="RosieViolationSquiggleTagger"/>.
    /// </summary>
    [TestFixture]
    public class RosieViolationSquiggleTaggerTest
    {
        [Test]
        public void GetTags_should_return_no_tag_for_no_span()
        {
            var tagAggregator = new Mock<ITagAggregator<RosieViolationTag>>();
            var tagger = new RosieViolationSquiggleTagger(tagAggregator.Object);
            var spanCollection =
                new NormalizedSnapshotSpanCollection(new Mock<ITextSnapshot>().Object, new List<Span>());

            var tagSpans = tagger.GetTags(spanCollection);

            Assert.That(tagSpans, Is.Empty);
        }

        [Test]
        public void GetTags_should_return_no_tag_when_the_tagger_is_already_disposed()
        {
            var tagAggregator = new Mock<ITagAggregator<RosieViolationTag>>();
            var tagger = new RosieViolationSquiggleTagger(tagAggregator.Object);
            var spanCollection = TaggingMockSupport.CreateSpanCollection(new Mock<ITextSnapshot>(), 0, 0);

            tagger.Dispose();

            var tagSpans = tagger.GetTags(spanCollection);

            Assert.That(tagSpans, Is.Empty);
        }

        [Test]
        public void GetTags_should_return_no_tag_for_no_rosie_violation_tag_aggregated()
        {
            var tagAggregator = new Mock<ITagAggregator<RosieViolationTag>>();
            tagAggregator
                .Setup(a => a.GetTags(It.IsAny<NormalizedSnapshotSpanCollection>()))
                .Returns(new List<IMappingTagSpan<RosieViolationTag>>());
            var tagger = new RosieViolationSquiggleTagger(tagAggregator.Object);
            var spanCollection = TaggingMockSupport.CreateSpanCollection(new Mock<ITextSnapshot>(), 0, 0);

            var tagSpans = tagger.GetTags(spanCollection);

            Assert.That(tagSpans, Is.Empty);
        }

        [Test]
        public void GetTags_should_return_correct_tags_having_filtered_out_tags_with_non_single_spans()
        {
            //Mock argument of GetTags()
            var textSnapshot = new Mock<ITextSnapshot>();
            textSnapshot.Setup(tss => tss.Length).Returns(50);
            var spanCollection = TaggingMockSupport.CreateSpanCollection(textSnapshot, 0, 0);

            //Mock ITagAggregator<RosieViolationTag>
            var tagAggregator = new Mock<ITagAggregator<RosieViolationTag>>();
            var mappingSpanEmpty =
                TaggingMockSupport.MockMappingSpan(textSnapshot, new NormalizedSnapshotSpanCollection());
            var mappingSpanSingle =
                TaggingMockSupport.MockMappingSpan(textSnapshot,
                    TaggingMockSupport.CreateSpanCollection(textSnapshot, 0, 0));
            var mappingSpanMulti =
                TaggingMockSupport.MockMappingSpan(textSnapshot, CreateMultiSpanCollection(textSnapshot));

            tagAggregator
                .Setup(a => a.GetTags(It.IsAny<NormalizedSnapshotSpanCollection>()))
                .Returns(new List<IMappingTagSpan<RosieViolationTag>>
                {
                    //Critical
                    CreateMappingTagSpan(mappingSpanEmpty, "critical rule filtered", "critical filtered message",
                        RosieSeverities.Critical),
                    CreateMappingTagSpan(mappingSpanSingle, "critical rule", "critical message",
                        RosieSeverities.Critical),
                    //Error
                    CreateMappingTagSpan(mappingSpanSingle, "error rule", "error message",
                        RosieSeverities.Error),
                    //Warning
                    CreateMappingTagSpan(mappingSpanMulti, "warning rule filtered", "warning filtered message",
                        RosieSeverities.Warning),
                    CreateMappingTagSpan(mappingSpanSingle, "warning rule", "warning message",
                        RosieSeverities.Warning),
                    //Informational
                    CreateMappingTagSpan(mappingSpanSingle, "informational rule", "informational message",
                        "informational"),
                    CreateMappingTagSpan(mappingSpanSingle, "unknown rule", "unknown message", "unknown"),
                });
            var tagger = new RosieViolationSquiggleTagger(tagAggregator.Object);

            var tagSpans = tagger.GetTags(spanCollection).ToList();

            Assert.That(tagSpans, Has.Count.EqualTo(5));
            Assert.Multiple(() =>
            {
                Assert.That(tagSpans[0].Tag.ErrorType, Is.EqualTo("Rosie Violation Critical"));
                Assert.That(tagSpans[0].Tag.ToolTipContent, Is.EqualTo("critical message"));

                Assert.That(tagSpans[1].Tag.ErrorType, Is.EqualTo("Rosie Violation Error"));
                Assert.That(tagSpans[1].Tag.ToolTipContent, Is.EqualTo("error message"));

                Assert.That(tagSpans[2].Tag.ErrorType, Is.EqualTo("Rosie Violation Warning"));
                Assert.That(tagSpans[2].Tag.ToolTipContent, Is.EqualTo("warning message"));

                Assert.That(tagSpans[3].Tag.ErrorType, Is.EqualTo("Rosie Violation Informational"));
                Assert.That(tagSpans[3].Tag.ToolTipContent, Is.EqualTo("informational message"));

                Assert.That(tagSpans[4].Tag.ErrorType, Is.EqualTo("Rosie Violation Informational"));
                Assert.That(tagSpans[4].Tag.ToolTipContent, Is.EqualTo("unknown message"));                
            });
        }

        #region Mock and test object creation

        /// <summary>
        /// Creates a <c>NormalizedSnapshotSpanCollection</c> with multiple spans to test if those are
        /// filtered out by the tag retrieval.
        /// </summary>
        private static NormalizedSnapshotSpanCollection CreateMultiSpanCollection(IMock<ITextSnapshot> textSnapshot)
        {
            //These offset values are arbitrary and not relevant for the tests 
            return new NormalizedSnapshotSpanCollection(textSnapshot.Object,
                new List<Span> { new Span(0, 5), new Span(6, 10) });
        }

        /// <summary>
        /// Mocks a single item returned by <c>ITagAggregator.GetTags(NormalizedSnapshotSpanCollection)</c>
        /// with the argument rule name, and message and severity set for the underlying <c>RosieViolation</c>.
        /// </summary>
        private static MappingTagSpan<RosieViolationTag> CreateMappingTagSpan(IMock<IMappingSpan> mappingSpan,
            string ruleName, string message, string severity)
        {
            return new MappingTagSpan<RosieViolationTag>(
                mappingSpan.Object,
                new RosieViolationTag(
                    new RosieAnnotation(ruleName, "ruleset-name",
                        new RosieViolation { Message = message, Severity = severity })));
        }

        #endregion
    }
}