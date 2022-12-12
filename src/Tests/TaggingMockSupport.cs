using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Moq;

namespace Tests
{
    /// <summary>
    /// Utility for creating mocks for tagging related tests.
    /// </summary>
    public static class TaggingMockSupport
    {
        public static Mock<IMappingSpan> MockMappingSpan(
            IMock<ITextSnapshot> textSnapshot,
            NormalizedSnapshotSpanCollection spanCollection)
        {
            var mappingSpan = new Mock<IMappingSpan>();
            mappingSpan
                .Setup(ms => ms.GetSpans(textSnapshot.Object))
                .Returns(spanCollection);
            return mappingSpan;
        }
        
        public static NormalizedSnapshotSpanCollection CreateSpanCollection(IMock<ITextSnapshot> textSnapshot,
            int start, int length)
        {
            return new NormalizedSnapshotSpanCollection(
                textSnapshot.Object,
                new List<Span> { new Span(start, length) });
        }
    }
}