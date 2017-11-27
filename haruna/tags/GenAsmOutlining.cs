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
    [ContentType("text/asm")]    
    public sealed class GenAsmOutlineTaggerProvider : ITaggerProvider
    {
        [Import]
        internal IClassifierAggregatorService classifierAggregator = null;

        #region ITaggerProvider Members

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            object ret = null;
            if (buffer.CurrentSnapshot.LineCount == 0)
            {
                ret = buffer.Properties.GetOrCreateSingletonProperty<GenAsmOutlineTaggerTagger>(() => {
                    return new GenAsmOutlineTaggerTagger();
                });
            }
            else
            {
                var line = buffer.CurrentSnapshot.GetLineFromLineNumber(0);
                switch (line.GetText().Trim().ToLower())
                {
                    case ";!nasm":
                        ret = buffer.Properties.GetOrCreateSingletonProperty<NasmOutlineTagger>(() => {
                            return new NasmOutlineTagger(buffer, classifierAggregator);
                        });
                        break;
                    case ";!masm":
                        ret = buffer.Properties.GetOrCreateSingletonProperty<MasmOutlineTagger>(() =>
                        {
                            return new MasmOutlineTagger(buffer, classifierAggregator);
                        });
                        break;
                    default:
                        ret = buffer.Properties.GetOrCreateSingletonProperty<GenAsmOutlineTaggerTagger>(() => {
                            return new GenAsmOutlineTaggerTagger();
                        });
                        break;
                }
            }

            return ret as ITagger<T>;
        }
         
        #endregion
    }


    public sealed class GenAsmOutlineTaggerTagger : ITagger<IOutliningRegionTag>
    {
        #region ITagger<OutliningRegionTag> Members

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return new ITagSpan<IOutliningRegionTag>[0];
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion        
    }
}
