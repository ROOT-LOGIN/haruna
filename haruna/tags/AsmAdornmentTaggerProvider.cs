/* This File Is Not Included In Build Process */

//#define HIDING_TEXT

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace haruna
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("text/asm")]
    //[ContentType("projection")]
    [TagType(typeof(IntraTextAdornmentTag))]
    internal sealed class AsmAdornmentTaggerProvider : IViewTaggerProvider
    {
        #pragma warning disable 649 // "field never assigned to" -- field is set by MEF.
        [Import]
        internal IBufferTagAggregatorFactoryService BufferTagAggregatorFactoryService;
        #pragma warning restore 649

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView == null)
                throw new ArgumentNullException("textView");

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (buffer != textView.TextBuffer)
                return null;

            return AsmAdornmentTagger.GetTagger(
                (IWpfTextView)textView,
                new Lazy<ITagAggregator<AsmTag>>(
                    () => BufferTagAggregatorFactoryService.CreateTagAggregator<AsmTag>(textView.TextBuffer)))
                as ITagger<T>;
        }
    }

    internal sealed class AsmAdornmentTagger

#if HIDING_TEXT
        : IntraTextAdornmentTagTransformer<AsmTag, UIElement>
#else
        : IntraTextAdornmentTagger<AsmTag, UIElement>
#endif

    {
        internal static ITagger<IntraTextAdornmentTag> GetTagger(IWpfTextView view, Lazy<ITagAggregator<AsmTag>> colorTagger)
        {
            return view.Properties.GetOrCreateSingletonProperty<AsmAdornmentTagger>(
                () => new AsmAdornmentTagger(view, colorTagger.Value));
        }

#if HIDING_TEXT
        private AsmAdornmentTagger(IWpfTextView view, ITagAggregator<AsmTag> colorTagger)
            : base(view, colorTagger)
        {
        }

        public override void Dispose()
        {
            base.view.Properties.RemoveProperty(typeof(AsmAdornmentTagger));
        }
#else
        private ITagAggregator<AsmTag> asmTagger;

        private AsmAdornmentTagger(IWpfTextView view, ITagAggregator<AsmTag> asmTagger)
            : base(view)
        {
            this.asmTagger = asmTagger;
        }

        public void Dispose()
        {
            this.asmTagger.Dispose();

            base.view.Properties.RemoveProperty(typeof(AsmAdornmentTagger));
        }

        // To produce adornments that don't obscure the text, the adornment tags
        // should have zero length spans. Overriding this method allows control
        // over the tag spans.
        protected override IEnumerable<Tuple<SnapshotSpan, PositionAffinity?, AsmTag>> GetAdornmentData(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            ITextSnapshot snapshot = spans[0].Snapshot;

            var asmTags = this.asmTagger.GetTags(spans);

            foreach (IMappingTagSpan<AsmTag> dataTagSpan in asmTags)
            {
                NormalizedSnapshotSpanCollection asmTagSpans = dataTagSpan.Span.GetSpans(snapshot);

                // Ignore data tags that are split by projection.
                // This is theoretically possible but unlikely in current scenarios.
                if (asmTagSpans.Count != 1)
                    continue;

                SnapshotSpan adornmentSpan = new SnapshotSpan(asmTagSpans[0].End, 0);

                yield return Tuple.Create(adornmentSpan, (PositionAffinity?)PositionAffinity.Successor, dataTagSpan.Tag);
            }
        }
#endif

        protected override UIElement CreateAdornment(AsmTag dataTag, SnapshotSpan span)
        {
            return dataTag.GetDisplay();
        }

        protected override bool UpdateAdornment(UIElement adornment, AsmTag dataTag)
        {
            
            return true;
        }
    }
}
