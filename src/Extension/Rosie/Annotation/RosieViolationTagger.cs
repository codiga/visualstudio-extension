using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extension.Rosie.Model;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Extension.Rosie.Annotation
{
    /// <summary>
    /// Provides tagging information for an <see cref="ITextBuffer"/>, so that a span of text can be
    /// marked/tagged with information about a code analysis violation returned from the Rosie server.
    /// <br/>
    /// Instances of this classes are created by <see cref="RosieViolationTaggerProvider"/>.
    /// </summary>
    public class RosieViolationTagger : ITagger<RosieViolationTag>, IDisposable
    {
        /// <summary>
        /// Called when there was a change in the edited document, to signal that tagging must be updated.
        /// <br/>
        /// Instantiated automatically by the VS platform.
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <summary>
        /// We wait for this amount of time after each edit in a text buffer, and if no edit happened during that
        /// interval, only then we send a request to Rosie. 
        /// </summary>
        private const double DocumentModificationWaitIntervalInMillis = 500;

        /// <summary>
        /// The text view whose text buffer is used for code analysis.
        /// </summary>
        private readonly ITextView _sourceView;

        /// <summary>
        /// The text buffer on whose changes requests to Rosie, and the persistence of Rosie annotations are performed.
        /// </summary>
        private readonly ITextBuffer? _sourceBuffer;

        /// <summary>
        /// Stores the timestamp (in milliseconds) of the text buffer's last modification.
        /// <br/>
        /// We use this timestamp instead of the file's last write time, because the last write time requires the file to be saved,
        /// not just edited, to change.
        /// </summary>
        private long _fileLastModificationTime;

        /// <summary>
        /// Stores the Rosie annotations, and violation details, returned from Rosie.
        /// <br/>
        /// Since there is a conceptual difference between how Rosie works and how tagging works, we have to do some caching.
        /// <br/>
        /// While Rosie is called for an entire file, <c>ITagger.GetTags()</c> is called for a span/range within a file,
        /// that can be be a span within a file, or the span of the whole file.
        /// So, during the creation of this tagger, and after each edit in a document (in an ITextBuffer, and if the user hasn't typed for
        /// at least 500ms), we send a Rosie request for the whole file, cache it in this field, and if there is no reason to re-analyze
        /// the file, we provide the information for tags from this field.
        /// </summary>
        public IList<RosieAnnotation> Annotations { get; set; } = RosieClient.NoAnnotation;

        private bool _isDisposed;
        private readonly TextBufferDataProvider? _dataProvider;

        //For testing
        public RosieViolationTagger(TextBufferDataProvider? dataProvider = null)
        {
            _dataProvider = dataProvider;
        }

        public RosieViolationTagger(ITextView sourceView, ITextBuffer sourceBuffer)
        {
            _dataProvider = new TextBufferDataProvider();
            _sourceView = sourceView;
            _sourceBuffer = sourceBuffer;

            //Preparing this tagger for code analysis only when all the Changed* event handlers have finished execution
            _sourceBuffer.PostChanged += RequestCodeAnalysis;

            //Remove event handler when the text view gets closed, so that code analysis doesn't happen on a closed view
            sourceView.Closed += (sender, args) => _sourceBuffer.PostChanged -= RequestCodeAnalysis;

            //Sends the first request to Rosie for this file,
            //so that we can display potential issues right after a file is opened/displayed.
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                if (RosieClientProvider.TryGetClient(out var client))
                    Annotations = await client.GetAnnotationsAsync(_sourceBuffer);
            });
        }

        /// <summary>
        /// It requests a code analysis, sends a request to Rosie for the current file, and caches its results in <see cref="Annotations"/>
        /// if the user hasn't typed anything in the last <see cref="DocumentModificationWaitIntervalInMillis"/>.
        /// <br/>
        /// When the violations are received from the server, it signals this tagger, thus all associated <see cref="ITagAggregator{T}"/>s
        /// to update the tags.
        /// <br/>
        /// Notes on asynchronicity:
        /// <br/>
        /// Although async void methods are advised for only certain types of usage, due to its error handling mechanism,
        /// there doesn't seem to be a different way to handle this event synchronously while still using <c>await</c>.
        /// <br/> 
        /// See details about <a href="https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming">async void methods</a>.
        /// <br/>
        /// The wait logic was adopted from the <a href="https://github.com/codiga/vscode-plugin/blob/main/src/diagnostics/diagnostics.ts">Codiga VSCode plugin</a>. 
        /// </summary>
        /// <param name="sender">the sender of the event</param>
        /// <param name="e">the event arguments</param>
        private async void RequestCodeAnalysis(object sender, EventArgs e)
        {
            // Save the timestamp, so that other threads and analysis requests can see it.
            var fileLastModificationTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _fileLastModificationTime = fileLastModificationTime;

            // Wait for some time. During that time, the user might type another key that triggers further buffer changed events,
            // that also update the saved timestamp.
            var delay = Task.Delay(TimeSpan.FromMilliseconds(DocumentModificationWaitIntervalInMillis),
                new CancellationToken());
            try
            {
                await delay;
            }
            catch (TaskCanceledException)
            {
                return;
            }

            // Get the actual saved timestamp, and check if it is the one we called the function with.
            // If yes, and it hasn't changed (the user finished typing for at least the wait time), request code analysis.
            if (_fileLastModificationTime == fileLastModificationTime)
                await UpdateAnnotationsAndNotifyTagsChangedAsync(_sourceView);
        }

        /// <summary>
        /// Sends the argument buffer's contents for code analysis,
        /// then signals a tag update for the whole file's range in the argument text buffer.
        /// </summary>
        /// <param name="textView">The text view in which the tagging needs to be updated.</param>
        public async Task UpdateAnnotationsAndNotifyTagsChangedAsync(ITextView textView)
        {
            if (RosieClientProvider.TryGetClient(out var client))
            {
                Annotations = await client.GetAnnotationsAsync(textView.TextBuffer);
                TagsChanged.Invoke(this,
                    new SnapshotSpanEventArgs(new SnapshotSpan(textView.TextBuffer.CurrentSnapshot,
                        new Span(0, textView.TextBuffer.CurrentSnapshot.Length == 0 ? 0 : textView.TextBuffer.CurrentSnapshot.Length - 1))));
                if (RosieRulesCache.Instance != null)
                    textView.Properties[RosieRulesCache.CacheLastUpdatedTimeStampProp] =
                        RosieRulesCache.Instance.CacheLastUpdatedTimeStamp;
            }
        }

        public /*override*/ IEnumerable<ITagSpan<RosieViolationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return ThreadHelper.JoinableTaskFactory.Run(async () => await GetTagsAsync(spans));
        }

        /// <summary>
        /// Returns the tag spans for the requested span. It creates new <see cref="RosieViolationTag"/>s for each <see cref="RosieAnnotation"/>
        /// <br/>
        /// It calculates the positions in the text buffer, based on the line and column coordinates in each Rosie violation,
        /// so that later, the error squiggles can be display in their correct positions and ranges.
        /// <br/>
        /// NOTE: this is visible only for testing.
        /// </summary>
        public async Task<IEnumerable<ITagSpan<RosieViolationTag>>> GetTagsAsync(
            NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || _isDisposed)
                return Enumerable.Empty<ITagSpan<RosieViolationTag>>();

            var span = spans[0];
            var snapshot = span.Snapshot;

            //If there is no file to create tags in, return no tags
            if (await GetFileNameAsync(snapshot) == null)
                return Enumerable.Empty<ITagSpan<RosieViolationTag>>();

            var tagSpans = new List<ITagSpan<RosieViolationTag>>();

            //Iterate over all Rosie annotations/violations and create the proper tags for each of them.
            foreach (var annotation in Annotations)
            {
                int annotationStart;
                int annotationEnd;
                try
                {
                    annotationStart = annotation.Start.GetOffset(snapshot.TextBuffer);
                    annotationEnd = annotation.End.GetOffset(snapshot.TextBuffer);
                }
                catch (ArgumentOutOfRangeException)
                {
                    //For example, when deleting an entire line in the document, and the not-yet-updated violation is on the last line of the document,
                    //the Line of RosiePosition can be greater than the updated line count of the text buffer. 
                    continue;
                }

                //If the spans don't intersect, or the annotation starts later than it ends, don't create a tag
                //span:             |--------|
                //anno:                          |--------|
                //anno: |--------|
                if (annotationStart > span.End.Position
                    || annotationEnd < span.Start.Position
                    || annotationStart > annotationEnd)
                    continue;

                if (annotationStart <= span.Start.Position)
                {
                    //span:    |--------------|
                    //anno:    |--------------|
                    //anno:    |------------------|
                    //anno:  |----------------|
                    //anno:  |------------------|
                    if (annotationEnd >= span.End.Position)
                        tagSpans.Add(new TagSpan<RosieViolationTag>(
                            new SnapshotSpan(snapshot, span.Start.Position, span.Length),
                            new RosieViolationTag(annotation)));
                    //span:    |--------------|
                    //anno: |--------------|
                    //anno:    |------------|
                    else
                        tagSpans.Add(new TagSpan<RosieViolationTag>(new SnapshotSpan(snapshot, span.Start.Position,
                            annotationEnd - span.Start.Position), new RosieViolationTag(annotation)));
                }
                else
                {
                    //span:    |--------------|
                    //anno:       |-----------|
                    //anno:       |----------------|
                    if (annotationEnd >= span.End.Position)
                        tagSpans.Add(new TagSpan<RosieViolationTag>(new SnapshotSpan(snapshot,
                            annotationStart,
                            span.End.Position - annotationStart), new RosieViolationTag(annotation)));
                    //span:    |--------------|
                    //anno:       |--------|
                    else
                        tagSpans.Add(new TagSpan<RosieViolationTag>(new SnapshotSpan(snapshot,
                                annotationStart,
                                annotationEnd - annotationStart),
                            new RosieViolationTag(annotation)));
                }
            }

            return tagSpans;
        }

        private async Task<string?> GetFileNameAsync(ITextSnapshot snapshot)
        {
            if (!_dataProvider.IsTestMode)
            {
                //Temporarily switching back to main thread due to RosieRulesCache.StartPolling()
                return await Task.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return snapshot.TextBuffer.GetFileName();
                });
            }

            return _dataProvider.FileName(snapshot.TextBuffer);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_sourceBuffer != null)
                    _sourceBuffer.PostChanged -= RequestCodeAnalysis;
                _isDisposed = true;
            }
        }
    }
}