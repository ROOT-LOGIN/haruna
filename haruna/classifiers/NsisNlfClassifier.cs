using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Linq;
using System.Diagnostics;
using System.Windows.Media;
using System.Collections.Specialized;
using haruna.mutable;

namespace haruna
{
    [Export(typeof(IClassifierProvider))]
    [ContentType("text/nlf")]
    public class NsisNlfClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService classificationRegistry = null; // Set via MEF

        #region IClassifierProvider Members

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            // derived type will also go through
            if(textBuffer.ContentType.TypeName != "text/nlf")
            {
                return null;
            }

            IClassifier ret = null;
            ret = textBuffer.Properties.GetOrCreateSingletonProperty(() =>
            {
                return new NsisNlfClassifier(textBuffer, classificationRegistry);
            });

            return ret as IClassifier;
        }
        #endregion
    }    

    internal class NsisNlfClassifier : IClassifier
    {
        protected readonly IClassificationType _commentClassificationType;
        protected readonly IClassificationType _stringClassificationType;
        protected readonly IClassificationType _macroClassificationType;

        protected SimpleAsmLineParser simpleAsmLineParser;
        protected readonly ITextBuffer m_textBuffer;

        protected Dictionary<int, string> m_Labels;
        public Dictionary<int, string> GetLabels()
        {
            return m_Labels;
        }

        public NsisNlfClassifier(ITextBuffer textBuffer, IClassificationTypeRegistryService registry)
        {
            m_textBuffer = textBuffer;
            m_Labels = new Dictionary<int, string>(23);

            _commentClassificationType = registry.GetClassificationType(Constants.classifier_nsis_comment);
            _stringClassificationType = registry.GetClassificationType(Constants.classifier_nsis_string);
            _macroClassificationType = registry.GetClassificationType(Constants.classifier_nsis_condition);

            Initialize();            
        }

        protected virtual void Initialize()
        {
            simpleAsmLineParser = new SimpleAsmLineParser(AsmClassifierFamilyType.General);
        }

        protected ClassificationSpan CreateClassificationSpan(SnapshotSpan span, int start, int length, IClassificationType clsty, ClassifierDefinition definition)
        {
            return new HurunaClassificationSpan(
                new SnapshotSpan(span.Snapshot, new Span(start, length)), clsty)
            { 
                Definition = definition 
            };
        }

        #region IClassifier Members

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public static ClassificationSpan[] s_EmptySpan = new ClassificationSpan[0];

        static bool isWhitespace(char c)
        {
            return c == ' ' || c == '\t' || c == '\v' || c == '\r' || c == '\n';
        }


        public virtual IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            if(m_textBuffer.EditInProgress || span.IsEmpty) return s_EmptySpan;            

            List<ClassificationSpan> classifications = new List<ClassificationSpan>();
            ClassificationSpan cspan = new ClassificationSpan(span, _stringClassificationType);
            var lines = span.GetText().TrimEnd();
            int baseoff = 0;
            foreach(var line in lines.Split('\n'))
            {
                if(line.Length == 0)
                {
                    baseoff++;
                    continue;
                }
                int off = 0;
                while (isWhitespace(line[off])) {
                    off++;
                }

                int skip = 1;
                switch(line[off])
                {
                    case '#':
                        {
                            off++;
                            while (isWhitespace(line[off])) {
                                off++;
                                skip++;
                            }

                            if(line.IndexOfAny(new []{ ' ', '\t', '\v' }, off) < 0)
                            {
                                classifications.Add(CreateClassificationSpan(span, span.Start + off - skip, skip, _commentClassificationType, new haruna.mutable.ClassifierDefinition()
                                {
                                    classifier = haruna.Constants.classifier_nsis_comment
                                }));
                                cspan = CreateClassificationSpan(span, span.Start + off, line.Length - off, _macroClassificationType, new haruna.mutable.ClassifierDefinition()
                                {
                                    classifier = haruna.Constants.classifier_nsis_condition
                                });
                            }
                            else
                            {
                                off -= skip;
                                cspan = CreateClassificationSpan(span, span.Start + off, line.Length - off, _commentClassificationType, new haruna.mutable.ClassifierDefinition()
                                {
                                    classifier = haruna.Constants.classifier_nsis_comment
                                });
                            }
                        }
                        break;
                    case ';':
                        {
                            cspan = CreateClassificationSpan(span, span.Start + off, line.Length - off, _commentClassificationType, new haruna.mutable.ClassifierDefinition()
                            {
                                classifier = haruna.Constants.classifier_nsis_comment
                            });
                        }
                        break;
                    default:
                        cspan = CreateClassificationSpan(span, span.Start + off, line.Length - off, _stringClassificationType, new haruna.mutable.ClassifierDefinition()
                        {
                            classifier = haruna.Constants.classifier_nsis_string
                        });
                        break;
                }
                classifications.Add(cspan);
            }
            return classifications;            
        }

        #endregion        
    }    
}
