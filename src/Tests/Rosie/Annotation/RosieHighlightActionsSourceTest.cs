using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extension.Rosie.Annotation;
using Extension.Rosie.Model;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using NUnit.Framework;

namespace Tests.Rosie.Annotation
{
    /// <summary>
    /// Unit test for <see cref="RosieHighlightActionsSource"/>.
    /// </summary>
    [TestFixture]
    public class RosieHighlightActionsSourceTest
    {
        #region HasSuggestedActionsAsync

        [Test]
        public async Task HasSuggestedActionsAsync_should_return_false_when_provider_is_disposed()
        {
            var tagAggregator = new Mock<ITagAggregator<RosieViolationTag>>();
            var actionsSource = new RosieHighlightActionsSource(tagAggregator.Object);

            actionsSource.Dispose();

            var hasSuggestedAction = await actionsSource.HasSuggestedActionsAsync(
                new Mock<ISuggestedActionCategorySet>().Object,
                new SnapshotSpan(new Mock<ITextSnapshot>().Object, 0, 0),
                new CancellationToken());

            Assert.That(hasSuggestedAction, Is.EqualTo(false));
        }

        [Test]
        public async Task HasSuggestedActionsAsync_should_return_false_when_no_aggregated_tag_is_returned()
        {
            var tagAggregator = MockTagAggregator(new List<IMappingTagSpan<RosieViolationTag>>());
            var actionsSource = new RosieHighlightActionsSource(tagAggregator.Object);

            var hasSuggestedAction = await actionsSource.HasSuggestedActionsAsync(
                new Mock<ISuggestedActionCategorySet>().Object,
                new SnapshotSpan(new Mock<ITextSnapshot>().Object, 0, 0),
                new CancellationToken());

            Assert.That(hasSuggestedAction, Is.EqualTo(false));
        }

        [Test]
        public async Task HasSuggestedActionsAsync_should_return_true_when_at_least_one_aggregated_tag_is_returned()
        {
            var mappingSpan =
                TaggingMockSupport.MockMappingSpan(new Mock<ITextSnapshot>(), new NormalizedSnapshotSpanCollection());
            var tagAggregator = MockTagAggregator(new List<IMappingTagSpan<RosieViolationTag>>
            {
                CreateMappingTagSpan(mappingSpan, "rule", new List<RosieViolationFix>())
            });
            var actionsSource = new RosieHighlightActionsSource(tagAggregator.Object);

            var hasSuggestedAction = await actionsSource.HasSuggestedActionsAsync(
                new Mock<ISuggestedActionCategorySet>().Object,
                new SnapshotSpan(new Mock<ITextSnapshot>().Object, 0, 0),
                new CancellationToken());

            Assert.That(hasSuggestedAction, Is.EqualTo(true));
        }

        #endregion

        #region GetSuggestedActions

        [Test]
        public void GetSuggestedActions_should_return_no_suggestion_for_already_disposed_actions_source()
        {
            var tagAggregator = new Mock<ITagAggregator<RosieViolationTag>>();
            var actionsSource = new RosieHighlightActionsSource(tagAggregator.Object);

            actionsSource.Dispose();

            var hasSuggestedAction = GetSuggestedActions(actionsSource);

            Assert.That(hasSuggestedAction, Is.Empty);
        }

        [Test]
        public void GetSuggestedActions_should_return_no_suggestion_for_no_violation_in_range()
        {
            var tagAggregator = MockTagAggregator(new List<IMappingTagSpan<RosieViolationTag>>()); 
            var actionsSource = new RosieHighlightActionsSource(tagAggregator.Object);

            var suggestedActions = GetSuggestedActions(actionsSource);

            Assert.That(suggestedActions, Is.Empty);
        }

        [Test]
        public void GetSuggestedActions_should_return_only_disable_and_open_actions_for_no_violation_fix_available()
        {
            var mappingSpan =
                TaggingMockSupport.MockMappingSpan(new Mock<ITextSnapshot>(), new NormalizedSnapshotSpanCollection());
            var tagAggregator = MockTagAggregator(new List<IMappingTagSpan<RosieViolationTag>>
            {
                CreateMappingTagSpan(mappingSpan, "ruleWithNofix", new List<RosieViolationFix>())
            });
            var actionsSource = new RosieHighlightActionsSource(tagAggregator.Object);

            var suggestedActions = GetSuggestedActions(actionsSource).ToList();

            Assert.That(suggestedActions, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                var actions = suggestedActions[0].Actions.ToList();
                Assert.That(actions, Has.Count.EqualTo(2));
                Assert.That(actions[0], Is.InstanceOf<DisableRosieAnalysisSuggestedAction>());
                Assert.That(actions[1], Is.InstanceOf<OpenOnCodigaHubSuggestedAction>());
            });
        }

        [Test]
        public void GetSuggestedActions_should_return_all_action_types()
        {
            var mappingSpan =
                TaggingMockSupport.MockMappingSpan(new Mock<ITextSnapshot>(), new NormalizedSnapshotSpanCollection());
            var tagAggregator = MockTagAggregator(new List<IMappingTagSpan<RosieViolationTag>>
            {
                CreateMappingTagSpan(mappingSpan, "ruleWithMultipleFixes",
                    new List<RosieViolationFix> { new RosieViolationFix(), new RosieViolationFix() }),
                CreateMappingTagSpan(mappingSpan, "ruleWithASingleFix",
                    new List<RosieViolationFix> { new RosieViolationFix() }),
            });
            var actionsSource = new RosieHighlightActionsSource(tagAggregator.Object);

            var suggestedActions = GetSuggestedActions(actionsSource).ToList();

            Assert.That(suggestedActions, Has.Count.EqualTo(1));

            var actions = suggestedActions[0].Actions.ToList();
            Assert.That(actions, Has.Count.EqualTo(7));
            Assert.Multiple(() =>
            {
                Assert.That(actions[0], Is.InstanceOf<ApplyRosieFixSuggestedAction>());
                Assert.That(actions[1], Is.InstanceOf<ApplyRosieFixSuggestedAction>());
                Assert.That(actions[2], Is.InstanceOf<DisableRosieAnalysisSuggestedAction>());
                Assert.That(actions[3], Is.InstanceOf<OpenOnCodigaHubSuggestedAction>());

                Assert.That(actions[4], Is.InstanceOf<ApplyRosieFixSuggestedAction>());
                Assert.That(actions[5], Is.InstanceOf<DisableRosieAnalysisSuggestedAction>());
                Assert.That(actions[6], Is.InstanceOf<OpenOnCodigaHubSuggestedAction>());
            });
        }

        #endregion

        #region Mock and test object creation

        /// <summary>
        /// Creates a mock <c>ITagAggregator&lt;RosieViolationTag></c> whose <c>GetTags(SnapshotSpan)</c> method returns
        /// the provided list of <c>IMappingTagSpan</c>s.
        /// </summary>
        private static Mock<ITagAggregator<RosieViolationTag>> MockTagAggregator(IEnumerable<IMappingTagSpan<RosieViolationTag>> mappingTagSpans)
        {
            var tagAggregator = new Mock<ITagAggregator<RosieViolationTag>>();
            tagAggregator
                .Setup(a => a.GetTags(It.IsAny<SnapshotSpan>()))
                .Returns(mappingTagSpans);
            return tagAggregator;
        }

        /// <summary>
        /// Mocks a single item returned by <c>ITagAggregator.GetTags(SnapshotSpan)</c>
        /// with the argument fixes set for the underlying <c>RosieViolation</c>.
        /// </summary>
        private static MappingTagSpan<RosieViolationTag> CreateMappingTagSpan(IMock<IMappingSpan> mappingSpan,
            string ruleName, IList<RosieViolationFix> fixes)
        {
            return new MappingTagSpan<RosieViolationTag>(
                mappingSpan.Object,
                new RosieViolationTag(
                    new RosieAnnotation(ruleName, "ruleset-name",
                        new RosieViolation { Fixes = fixes })));
        }

        /// <summary>
        /// Calls <c>GetSuggestedActions()</c> on the argument <c>RosieHighlightActionsSource</c> with some mock argumets.
        /// </summary>
        private static IEnumerable<SuggestedActionSet> GetSuggestedActions(RosieHighlightActionsSource actionsSource)
        {
            return actionsSource.GetSuggestedActions(
                new Mock<ISuggestedActionCategorySet>().Object,
                new SnapshotSpan(new Mock<ITextSnapshot>().Object, 0, 0),
                new CancellationToken());
        }

        #endregion
    }
}