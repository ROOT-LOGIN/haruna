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
using System.Windows;

namespace haruna
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_asm_instruction)]
    [Name("asm/instructionformat")]
    [DisplayName("asm.instructionformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class AsmInstructionClassificationFormat : ClassificationFormatDefinition
    {
        public AsmInstructionClassificationFormat( )
        {
            base.DisplayName = "asm - instruction";
            base.ForegroundColor = Colors.Blue;
            base.IsItalic = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_asm_register)]
    [Name("asm/registerformat")]
    [DisplayName("asm.registerformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class AsmRegisterClassificationFormat : ClassificationFormatDefinition
    {
        public AsmRegisterClassificationFormat( )
        {
            base.DisplayName = "asm - register";
            base.ForegroundColor = Colors.DarkCyan;
            //base.IsBold = true;
        }
    }    

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_asm_pseudo)]
    [Name("asm/pseudoformat")]
    [DisplayName("asm.pseudoformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class AsmPseudoClassificationFormat : ClassificationFormatDefinition
    {
        public AsmPseudoClassificationFormat( )
        {
            base.DisplayName = "asm - pseudo";
            base.ForegroundColor = Colors.Brown;
            //base.IsItalic = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_asm_intrinsic)]
    [Name("asm/intrinsicformat")]
    [DisplayName("asm.intrinsicformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class AsmIntrinsicClassificationFormat : ClassificationFormatDefinition
    {
        public AsmIntrinsicClassificationFormat( )
        {
            base.DisplayName = "asm - intrinsic";
            base.ForegroundColor = Colors.Purple;
            //base.IsBold = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_asm_comment)]
    [Name("asm/commentformat")]
    [DisplayName("asm.commentformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class AsmCommentClassificationFormat : ClassificationFormatDefinition
    {
        public AsmCommentClassificationFormat( )
        {
            base.DisplayName = "asm - comment";
            base.ForegroundColor = Colors.Green;
            //base.IsBold = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_asm_address)]
    [Name("asm/addressformat")]
    [DisplayName("asm.addressformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class AsmAddressClassificationFormat : ClassificationFormatDefinition
    {
        public AsmAddressClassificationFormat( )
        {
            base.DisplayName = "asm - address";
            base.ForegroundColor = Colors.Gray;
            base.IsBold = true;
            //base.TextDecorations = System.Windows.TextDecorations.Underline;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_asm_label)]
    [Name("asm/labelformat")]
    [DisplayName("asm.labelformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class AsmLabelClassificationFormat : ClassificationFormatDefinition
    {
        public AsmLabelClassificationFormat( )
        {
            base.DisplayName = "asm - label";
            base.ForegroundColor = Colors.Gray;
            base.IsBold = true;
            //base.TextDecorations = System.Windows.TextDecorations.Underline;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_asm_locallabel)]
    [Name("asm/locallabelformat")]
    [DisplayName("asm.locallabelformat")]
    [UserVisible(false)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class AsmLocalLabelClassificationFormat : ClassificationFormatDefinition
    {
        public AsmLocalLabelClassificationFormat( )
        {
            base.DisplayName = "asm - locallabel";
            base.ForegroundColor = Colors.Gray;
            base.IsBold = false;
            base.TextDecorations = System.Windows.TextDecorations.Underline;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_asm_cxxdecname)]
    [Name("asm/cxxdecnameformat")]
    [DisplayName("asm.cxxdecnameformat")]
    [UserVisible(false)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class AsmCxxDecdameClassificationFormat : ClassificationFormatDefinition
    {
        public AsmCxxDecdameClassificationFormat()
        {
            base.DisplayName = "asm - cxxdecname";
            //base.ForegroundColor = Colors.Black;
            //base.IsBold = true;
            //base.TextDecorations = System.Windows.TextDecorations.Baseline;
            base.FontTypeface = new Typeface("Courier New");
        }
    }


    // =====================================================


    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_nsis_instruction)]
    [Name("nsis/instructionformat")]
    [DisplayName("nsis.instructionformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class NsisInstructionClassificationFormat : ClassificationFormatDefinition
    {
        public NsisInstructionClassificationFormat()
        {
            base.DisplayName = "asm - instruction";
            base.ForegroundColor = Colors.Orange;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_nsis_intrinsic)]
    [Name("nsis/intrinsicformat")]
    [DisplayName("nsis.intrinsicformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class NsisIntrinsicClassificationFormat : ClassificationFormatDefinition
    {
        public NsisIntrinsicClassificationFormat()
        {
            base.DisplayName = "nsis - intrinsic";
            base.ForegroundColor = Colors.Gray;
            base.IsBold = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_nsis_comment)]
    [Name("nsis/commentformat")]
    [DisplayName("nsis.commentformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class NsisCommentClassificationFormat : ClassificationFormatDefinition
    {
        public NsisCommentClassificationFormat()
        {
            base.DisplayName = "nsis - comment";
            base.ForegroundColor = Colors.Green;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_nsis_token)]
    [Name("nsis/tokenformat")]
    [DisplayName("nsis.tokenformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class NsisTokenClassificationFormat : ClassificationFormatDefinition
    {
        public NsisTokenClassificationFormat()
        {
            base.DisplayName = "nsis - token";
            base.ForegroundColor = Colors.Blue;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_nsis_attribute)]
    [Name("nsis/attributeformat")]
    [DisplayName("nsis.attributeformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class NsisAttributeClassificationFormat : ClassificationFormatDefinition
    {
        public NsisAttributeClassificationFormat()
        {
            base.DisplayName = "asm - attribute";
            base.ForegroundColor = Colors.Orange;
            base.IsBold = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_nsis_condition)]
    [Name("nsis/conditionformat")]
    [DisplayName("nsis.conditionformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class NsisConditionClassificationFormat : ClassificationFormatDefinition
    {
        public NsisConditionClassificationFormat()
        {
            base.DisplayName = "nsin - condition";
            base.ForegroundColor = Colors.DarkMagenta;
            base.IsBold = true;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Constants.classifier_nsis_string)]
    [Name("nsis/stringformat")]
    [DisplayName("nsis.stringformat")]
    [UserVisible(true)]
    [Order(After = Priority.Default, Before = Priority.High)]
    internal sealed class NsisStringClassificationFormat : ClassificationFormatDefinition
    {
        public NsisStringClassificationFormat()
        {
            base.DisplayName = "nsin - string";
            base.ForegroundColor = Colors.Maroon;
        }
    }


}
