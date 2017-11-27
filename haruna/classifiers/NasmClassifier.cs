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

namespace haruna
{    
    [Export(typeof(IClassifierProvider))]    
    [ContentType("text/nasm")]
    public class NasmClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService classificationRegistry = null; // Set via MEF
        
        #region IClassifierProvider Members

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(()=>{
                return new NasmClassifier(textBuffer, classificationRegistry);
            });
        }

        #endregion
    }    

    internal class NasmClassifier : GeneralAsmClassifier
    {               
        public NasmClassifier(ITextBuffer textBuffer, IClassificationTypeRegistryService registry)
            : base(textBuffer, registry)
        {            
        }

        protected override void Initialize( )
        {
            simpleAsmLineParser = new SimpleAsmLineParser(AsmClassifierFamilyType.Nasm);
        }

        #region IClassifier Members                        

        public override IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            if(m_textBuffer.EditInProgress || span.IsEmpty) return s_EmptySpan;
            
            List<ClassificationSpan> classifications = new List<ClassificationSpan>();            
            var line = span.GetText().TrimEnd();            
            simpleAsmLineParser.Parse(line, (definition, clsstr, start, length) =>
            {
                int l_key = span.Snapshot.GetLineNumberFromPosition(span.Start + start);
                IClassificationType clsty = null;
                if(clsstr == Constants.classifier_asm_register) clsty = _registerClassificationType;
                else if(clsstr == Constants.classifier_asm_pseudo) clsty = _pseudoClassificationType;
                else if(clsstr == Constants.classifier_asm_intrinsic)
                {
                    clsty = _intrinsicClassificationType;
                    switch(line.Substring(start, length).ToLower())
                    {
                        case "%comment":
                        case "%endcomment":              
                            clsty = _commentClassificationType;
                        break;
                    }                    
                }
                else if(clsstr == Constants.classifier_asm_instruction) clsty = _instructionClassificationType;
                else if(clsstr == Constants.classifier_asm_address) clsty = _addressClassificationType;
                else if(clsstr == Constants.classifier_asm_comment) clsty = _commentClassificationType;
                else if(clsstr == Constants.classifier_asm_label)
                {
                    clsty = _labelClassificationType;
                    if(line[start] == '.')
                    {
                        clsty = _locallabelClassificationType;
                    }
                }
                else if (clsstr == Constants.classifier_asm_cxxdecname) clsty = _cxxdecnameClassificationType;

                if (clsty == _labelClassificationType)
                {
                    m_Labels[l_key] = line.Substring(start, length);
                }
                else
                {
                    m_Labels.Remove(l_key);
                }
                if(clsty != null)
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
