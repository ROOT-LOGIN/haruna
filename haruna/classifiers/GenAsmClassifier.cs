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
    [ContentType("text/asm")]
    public class GeneralAsmClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService classificationRegistry = null; // Set via MEF

        #region IClassifierProvider Members

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            // derived type will also go through
            if(textBuffer.ContentType.TypeName != "text/asm")
            {
                return null;
            }

            IClassifier ret = null;
            var line = textBuffer.CurrentSnapshot.GetLineFromLineNumber(0);
            if (textBuffer.CurrentSnapshot.LineCount == 0)
            {
                ret = textBuffer.Properties.GetOrCreateSingletonProperty(() =>
                {
                    return new GeneralAsmClassifier(textBuffer, classificationRegistry);
                });
            }
                
            switch (line.GetText().Trim().ToLower())
            {
                case ";!nasm":
                    ret = textBuffer.Properties.GetOrCreateSingletonProperty(() =>
                    {
                        return new NasmClassifier(textBuffer, classificationRegistry);
                    });
                    break;
                case ";!masm":
                    ret = textBuffer.Properties.GetOrCreateSingletonProperty(() =>
                    {
                        return new MasmClassifier(textBuffer, classificationRegistry);
                    });
                    break;
                default:
                    ret = textBuffer.Properties.GetOrCreateSingletonProperty(() =>
                    {
                        return new GeneralAsmClassifier(textBuffer, classificationRegistry);
                    });
                    break;
            }

            return ret as IClassifier;
        }
        #endregion
    }    

    internal class HurunaClassificationSpan : ClassificationSpan
    {
        public HurunaClassificationSpan(SnapshotSpan span, IClassificationType classification) : base(span, classification) { }

        public ClassifierDefinition Definition { get; set; }
    }

    internal class GeneralAsmClassifier : IClassifier
    {
        protected readonly IClassificationType _registerClassificationType;
        protected readonly IClassificationType _pseudoClassificationType;        
        protected readonly IClassificationType _intrinsicClassificationType;
        protected readonly IClassificationType _instructionClassificationType;
        protected readonly IClassificationType _commentClassificationType;
        protected readonly IClassificationType _addressClassificationType;
        protected readonly IClassificationType _labelClassificationType;
        protected readonly IClassificationType _locallabelClassificationType;
        protected readonly IClassificationType _cxxdecnameClassificationType;

        protected SimpleAsmLineParser simpleAsmLineParser;
        protected readonly ITextBuffer m_textBuffer;

        protected Dictionary<int, string> m_Labels;
        public Dictionary<int, string> GetLabels()
        {
            return m_Labels;
        }

        public GeneralAsmClassifier(ITextBuffer textBuffer, IClassificationTypeRegistryService registry)
        {
            m_textBuffer = textBuffer;
            m_Labels = new Dictionary<int, string>(23);

            _instructionClassificationType = registry.GetClassificationType(Constants.classifier_asm_instruction);
            _registerClassificationType = registry.GetClassificationType(Constants.classifier_asm_register);
            _pseudoClassificationType = registry.GetClassificationType(Constants.classifier_asm_pseudo);
            _intrinsicClassificationType = registry.GetClassificationType(Constants.classifier_asm_intrinsic);
            _commentClassificationType = registry.GetClassificationType(Constants.classifier_asm_comment);
            _addressClassificationType = registry.GetClassificationType(Constants.classifier_asm_address);
            _labelClassificationType = registry.GetClassificationType(Constants.classifier_asm_label);
            _locallabelClassificationType = registry.GetClassificationType(Constants.classifier_asm_locallabel);
            _cxxdecnameClassificationType = registry.GetClassificationType(Constants.classifier_asm_cxxdecname);

            Initialize();            
        }

        public static IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            IClassifier classifier;
            if (!textBuffer.Properties.TryGetProperty(typeof(NasmClassifier), out classifier))
            {
                if (!textBuffer.Properties.TryGetProperty(typeof(MasmClassifier), out classifier))
                {
                    textBuffer.Properties.TryGetProperty(typeof(GeneralAsmClassifier), out classifier);
                }
            }
            return classifier;
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
                
        public virtual IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            if(m_textBuffer.EditInProgress || span.IsEmpty) return s_EmptySpan;
            
            List<ClassificationSpan> classifications = new List<ClassificationSpan>();            
            var line = span.GetText().TrimEnd();            
            simpleAsmLineParser.Parse(line, (definition, clsstr, start, length) =>
            {
                int l_key = span.Snapshot.GetLineNumberFromPosition(span.Start + start);
                IClassificationType clsty = null;
                if (clsstr == Constants.classifier_asm_register) clsty = _registerClassificationType;
                else if (clsstr == Constants.classifier_asm_pseudo) clsty = _pseudoClassificationType;
                else if (clsstr == Constants.classifier_asm_intrinsic) clsty = _intrinsicClassificationType;
                else if (clsstr == Constants.classifier_asm_instruction) clsty = _instructionClassificationType;
                else if (clsstr == Constants.classifier_asm_address) clsty = _addressClassificationType;
                else if (clsstr == Constants.classifier_asm_comment) clsty = _commentClassificationType;
                else if (clsstr == Constants.classifier_asm_label) clsty = _labelClassificationType;
                else if (clsstr == Constants.classifier_asm_cxxdecname) clsty = _cxxdecnameClassificationType;

                if (clsty == _labelClassificationType)
                {
                    m_Labels[l_key] = line.Substring(start, length);
                }
                else
                {
                    m_Labels.Remove(l_key);
                }

                if (clsty != null)
                {
                    classifications.Add(CreateClassificationSpan(
                        span, span.Start + start, length, clsty, definition));
                }
            });
            return classifications;            
        }

        #endregion        
    }    
}
