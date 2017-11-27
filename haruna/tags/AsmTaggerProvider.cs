/* This File Is Not Included In Build Process */

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Globalization;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Classification;
using System.Windows;
using System.Windows.Controls;

namespace haruna
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(AsmTag))]
    internal sealed class AsmTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            return buffer.Properties.GetOrCreateSingletonProperty<AsmTagger>(() => new AsmTagger(buffer)) as ITagger<T>;
        }
    }

    internal sealed class AsmTagger : AsmTaggerBase<AsmTag>
    {
        IClassifier m_classifier;

        internal AsmTagger(ITextBuffer textBuffer) : base(textBuffer)
        {
            m_classifier = GeneralAsmClassifier.GetClassifier(textBuffer);
        }

        public override IEnumerable<ITagSpan<AsmTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach(var span in spans)
            {
                var spanCls = m_classifier.GetClassificationSpans(span);
                foreach (var cls in spanCls)
                {
                    if(cls.ClassificationType.Classification == Constants.classifier_asm_cxxdecname)
                    {
                        yield return new TagSpan<AsmTag>(cls.Span, new CxxDecNameTag(cls.Span));
                    }
                }
            }
        }
    }

    internal abstract class AsmTag : ITag
    {
        protected readonly SnapshotSpan m_span;

        public AsmTag(SnapshotSpan span)
        {
            m_span = span;
        }

        public abstract UIElement GetDisplay();        
    }

    internal class CxxDecNameTag : AsmTag
    {
        public CxxDecNameTag(SnapshotSpan span) : base(span)
        {

        }

        string m_decname;
        public override UIElement GetDisplay()
        {
            if(m_decname == null)
            {
                if(!HurunaHelper.GetCxxUndecName(m_span.GetText(), out m_decname))
                {
                    m_decname = string.Empty;
                }
            }
            return new TextBlock() { Text = m_decname, Background = Brushes.AliceBlue };
        }
    }

}
