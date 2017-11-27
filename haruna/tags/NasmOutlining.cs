using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Linq;
using System.Diagnostics;

namespace haruna
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("text/nasm")]    
    public sealed class NasmOutlineTaggerProvider : ITaggerProvider
    {
        [Import]
        internal IClassifierAggregatorService classifierAggregator = null;

        #region ITaggerProvider Members

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty<NasmOutlineTagger>(
                ( ) => new NasmOutlineTagger(buffer, classifierAggregator)) as ITagger<T>;
        }
         
        #endregion
    }


    public sealed class NasmOutlineTagger : ITagger<IOutliningRegionTag>
    {
        IClassifierAggregatorService _classifierAggregator;

        static readonly string[] MacroKeywords = new string[] { "%macro", "%endmacro" };
        static readonly string[] StructKeywords = new string[] { "%struct", "%endstruct" };
        static readonly string[] CommentKeywords = new string[] { "%comment", "%endcomment" };
        static readonly string[] ConditionKeywords = new string[] {"%if", "%elif", "%else", "%endif"};

        ITextBuffer buffer;
        ITextSnapshot snapshot;
        List<Region> regions;

        public NasmOutlineTagger(ITextBuffer buffer, IClassifierAggregatorService classifierAggregator)
        {
            _classifierAggregator = classifierAggregator;

            this.buffer = buffer;
            this.snapshot = buffer.CurrentSnapshot;
            this.regions = new List<Region>();
            this.ReParse();
            // this makes sure outling region updated
            // but it will decrease performance
            this.buffer.ChangedLowPriority += BufferChanged; 
        }

        void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            if(buffer.EditInProgress) return;            
            
            if(e.After != buffer.CurrentSnapshot)
                return;
            
            if(!e.Changes.IncludesLineChanges || e.Changes.Count != 1) 
                return;

            var change = e.Changes[0];
            if(change.LineCountDelta > 0)
            {
                
            }
            else if(change.LineCountDelta < 0)
            {
            }
            this.ReParse();
        }

        #region ITagger<OutliningRegionTag> Members

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
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
                if(region.StartLine <= endLineNumber &&
                    region.EndLine >= startLineNumber)
                {
                    var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
                    var endLine = currentSnapshot.GetLineFromLineNumber(region.EndLine);

                    string ellipsis = HurunaHelper.Ellipsis;
                    string hoverText = HurunaHelper.GetHoverText(currentSnapshot, region);

                    yield return new TagSpan<IOutliningRegionTag>(
                        new SnapshotSpan(startLine.Start + region.StartOffset,
                        endLine.End),
                        new OutliningRegionTag(region.Type == RegionType.RT_Comment, true, ellipsis, hoverText));
                }
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

        Stack<Region> m_regionStack = new Stack<Region>();
        
        const int parse_success = 1; // a keyword has matched, and a region is been opened
        const int parse_skip = 0;    // a keyword has matched, but it should be ignored
        const int parse_fail = -1;   // a keyword hasn't been matched

        void ReParse()
        {
            m_regionStack.Clear();

            ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
            List<Region> newRegions = new List<Region>();

            //var classifier = _classifierAggregator.GetClassifier(this.buffer);
            foreach(var line in newSnapshot.Lines)
            {
                /*if(classifier != null)
                {
                    var x = classifier.GetClassificationSpans(new SnapshotSpan(line.Start, line.Length));
                    x = x;
                }  */              

                if(ReParse_Comment(line, newRegions) != parse_fail) continue;
                if(m_regionStack.Count > 0 && m_regionStack.Peek().Type == RegionType.RT_Comment)
                {
                    continue;
                }

                if(ReParse_Struct(line, newRegions) == parse_fail)
                {
                    if(ReParse_Macro(line, newRegions) == parse_fail)
                    {
                        ReParse_Condition(line, newRegions);
                    }
                }
            }

            newRegions = newRegions.Where(r => r.EndLine > -1).OrderBy(r => r.StartLine).ToList();
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

        bool CompareKeyword(string text, string keyword)
        {            
            int regionStart = text.IndexOf(keyword, StringComparison.InvariantCultureIgnoreCase);
            if(regionStart < 0) return false;
            
            int signStart = SimpleAsmLineParser.SkipSpace(text, 0);
            if(regionStart != signStart) return false;

            if(regionStart + keyword.Length >= text.Length) return true;

            return char.IsWhiteSpace(text[regionStart + keyword.Length]);
        }

        int ReParse_Comment(ITextSnapshotLine line, List<Region> newRegions)
        {
            string text = line.GetText();

            Region rgn = null; 
            // %comment, open a new region
            if(CompareKeyword(text, CommentKeywords[0]))
            {
                // %comment can't be nested
                if(m_regionStack.Count > 0 && m_regionStack.Peek().Type == RegionType.RT_Comment)
                {
                    return parse_skip;
                }

                m_regionStack.Push(new Region()
                {
                    Type = RegionType.RT_Comment,
                    StartLine = line.LineNumber,
                    StartOffset = line.Length
                });
            }
            // %endcomment, close current region
            else if(CompareKeyword(text, CommentKeywords[1]))
            {
                // %comment must exist
                if(m_regionStack.Count == 0 || m_regionStack.Peek().Type != RegionType.RT_Comment)
                {
                    return parse_skip;
                }

                rgn = m_regionStack.Pop();
                rgn.EndLine = line.LineNumber;
                newRegions.Add(rgn);
            }
            else
            {
                return parse_fail;
            }
            return parse_success;
        }

        int ReParse_Struct(ITextSnapshotLine line, List<Region> newRegions)
        {
            string text = line.GetText();

            Region rgn = null;
            // %struct, open a new region
            if(CompareKeyword(text, StructKeywords[0]))
            {
                // %struct can't be nested
                if(m_regionStack.Count > 0 && m_regionStack.Peek().Type == RegionType.RT_Struct)
                {
                    return parse_skip;
                }

                m_regionStack.Push(new Region()
                {
                    Type = RegionType.RT_Struct,
                    StartLine = line.LineNumber,
                    StartOffset = line.Length
                });
            }
            // %endstruct, close current region
            else if(CompareKeyword(text, StructKeywords[1]))
            {
                // %struct must exist
                if(m_regionStack.Count == 0 || m_regionStack.Peek().Type != RegionType.RT_Struct)
                {
                    return parse_skip;
                }

                rgn = m_regionStack.Pop();
                rgn.EndLine = line.LineNumber;
                newRegions.Add(rgn);
            }
            else
            {
                return parse_fail;
            }
            return parse_success;
        }

        int ReParse_Macro(ITextSnapshotLine line, List<Region> newRegions)
        {
            string text = line.GetText();

            Region rgn = null;
            // %macro, open a new region
            if(CompareKeyword(text, MacroKeywords[0]))
            {
                // %macro can't be nested
                if(m_regionStack.Count > 0 && m_regionStack.Peek().Type == RegionType.RT_Macro)
                {
                    return parse_skip;
                }

                m_regionStack.Push(new Region()
                {
                    Type = RegionType.RT_Macro,
                    StartLine = line.LineNumber,
                    StartOffset = line.Length
                });
            }
            // %endmacro, close current region
            else if(CompareKeyword(text, MacroKeywords[1]))
            {
                // %endmacro must exist
                if(m_regionStack.Count == 0 || m_regionStack.Peek().Type != RegionType.RT_Macro)
                {
                    return parse_skip;
                }

                rgn = m_regionStack.Pop();
                rgn.EndLine = line.LineNumber;
                newRegions.Add(rgn);
            }
            else
            {
                return parse_fail;
            }
            return parse_success;
        }

        int ReParse_Condition(ITextSnapshotLine line, List<Region> newRegions)
        {
            string text = line.GetText();

            Region rgn = null;    
            // %if, open a new region
            if(CompareKeyword(text, ConditionKeywords[0]))
            {
                m_regionStack.Push(new Region()
                {
                    Type = RegionType.RT_Condition,
                    StartLine = line.LineNumber,
                    StartOffset = line.Length
                });
            }
            // %endif, close current region
            else if(CompareKeyword(text, ConditionKeywords[3]))
            {
                if(m_regionStack.Count == 0) return parse_skip;

                rgn = m_regionStack.Pop();
                rgn.EndLine = line.LineNumber;
                newRegions.Add(rgn);
            }
            // %elif. %else, close current region and open a new region
            else if(CompareKeyword(text, ConditionKeywords[1]) || 
                CompareKeyword(text, ConditionKeywords[2]))
            {
                if(m_regionStack.Count == 0) return parse_skip;

                rgn = m_regionStack.Pop();
                rgn.EndLine = line.LineNumber - 1;
                newRegions.Add(rgn);
                m_regionStack.Push(new Region()
                {
                    Type = RegionType.RT_Condition,
                    StartLine = line.LineNumber,
                    StartOffset = line.Length
                });
            }
            else
            {
                return parse_fail;
            }
            return parse_success;
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
