using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Linq;

/*
 * THIS FILE IS A STUB, AND NOT INTEND FOR COMPILING
 */
namespace haruna
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(SpaceNegotiatingAdornmentTag))]
    [ContentType("text")]
    [FileExtension(".asm")]
    public sealed class LineAdornmentTaggerProvider : ITaggerProvider
    {
        #region ITaggerProvider Members

        static IViewTagAggregatorFactoryService ll;
        static IViewClassifierAggregatorService lll;
        static IBufferTagAggregatorFactoryService kk;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new LineAdornmentTagger(buffer) as ITagger<T>;

            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(
                ( ) => new LineAdornmentTagger(buffer) as ITagger<T>);
        }

        #endregion
    }


    public sealed class LineAdornmentTagger : ITagger<SpaceNegotiatingAdornmentTag>
    {
        const string startHide = "%comment";
        const string endHide = "%endcomment";        

        ITextBuffer buffer;
        ITextSnapshot snapshot;
        List<Region> regions;

        public LineAdornmentTagger(ITextBuffer buffer)
        {
            this.buffer = buffer;
            this.snapshot = buffer.CurrentSnapshot;
            this.regions = new List<Region>();
            this.ReParse();
            //this.buffer.Changed += BufferChanged;
        }

        void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event). 
            if(e.After != buffer.CurrentSnapshot)
                return;
            this.ReParse();
        }

        #region ITagger<OutliningRegionTag> Members

        public IEnumerable<ITagSpan<SpaceNegotiatingAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if(this.buffer.EditInProgress || spans.Count == 0)
                yield break;
            List<Region> currentRegions = this.regions;
            ITextSnapshot currentSnapshot = this.snapshot;
            SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End)
                .TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
            int startLineNumber = entire.Start.GetContainingLine().LineNumber;
            int endLineNumber = entire.End.GetContainingLine().LineNumber;
            foreach(var region in currentRegions)
            {
                var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
                var endLine = currentSnapshot.GetLineFromLineNumber(region.EndLine);

                //the region starts at the beginning of the "[", and goes until the *end* of the line that contains the "]".
                yield return new TagSpan<SpaceNegotiatingAdornmentTag>(
                    new SnapshotSpan(startLine.Start + region.StartOffset,
                    endLine.End),
                    new SpaceNegotiatingAdornmentTag(120.0, 32.0, 0.0, 14.0, 0.0, PositionAffinity.Predecessor, Guid.NewGuid(), Guid.NewGuid()));
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion

        static bool TryGetLevel(string text, int startIndex, out int level)
        {
            level = -1;
            if(text.Length > startIndex + 3)
            {
                if(int.TryParse(text.Substring(startIndex + 1), out level))
                    return true;
            }

            return false;
        }

        void ReParse( )
        {
            ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
            List<Region> newRegions = new List<Region>();

            foreach(var line in newSnapshot.Lines)
            {
                string text = line.GetText();

                newRegions.Add(new Region()
                {
                    StartLine = line.LineNumber,
                    EndLine = line.LineNumber,
                    StartOffset = 0
                });
            }
                
            //determine the changed span, and send a changed event with the new spans
            List<Span> oldSpans =
                new List<Span>(this.regions.Select(r => AsSnapshotSpan(r, this.snapshot)
                    .TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive)
                    .Span));
            List<Span> newSpans =
                    new List<Span>(newRegions.Select(r => AsSnapshotSpan(r, newSnapshot).Span));

            NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpans);
            NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpans);

            //the changed regions are regions that appear in one set or the other, but not both.
            NormalizedSpanCollection removed =
            NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

            int changeStart = int.MaxValue;
            int changeEnd = -1;

            if(removed.Count > 0)
            {
                changeStart = removed[0].Start;
                changeEnd = removed[removed.Count - 1].End;
            }

            if(newSpans.Count > 0)
            {
                changeStart = Math.Min(changeStart, newSpans[0].Start);
                changeEnd = Math.Max(changeEnd, newSpans[newSpans.Count - 1].End);
            }

            this.snapshot = newSnapshot;
            this.regions = newRegions;

            if(changeStart <= changeEnd)
            {
                ITextSnapshot snap = this.snapshot;
                if(this.TagsChanged != null)
                    this.TagsChanged(this, new SnapshotSpanEventArgs(
                        new SnapshotSpan(this.snapshot, Span.FromBounds(changeStart, changeEnd))));
            }
        }

        static SnapshotSpan AsSnapshotSpan(Region region, ITextSnapshot snapshot)
        {
            var startLine = snapshot.GetLineFromLineNumber(region.StartLine);
            var endLine = (region.StartLine == region.EndLine) ? startLine
                 : snapshot.GetLineFromLineNumber(region.EndLine);
            return new SnapshotSpan(startLine.Start + region.StartOffset, endLine.End);
        }
    }
}
