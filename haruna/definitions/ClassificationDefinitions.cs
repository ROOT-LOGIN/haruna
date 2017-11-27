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
    public sealed partial class ClassificationDefinitions
    {
        [Export]
        [Name(Constants.classifier_asm_instruction)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition asmInstructionDefinition;

        [Export]
        [Name(Constants.classifier_asm_register)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition asmRegisterDefinition;

        [Export]
        [Name(Constants.classifier_asm_pseudo)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition asmPesudoDefinition;

        [Export]
        [Name(Constants.classifier_asm_intrinsic)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition asmIntrinsicDefinition;

        [Export]
        [Name(Constants.classifier_asm_comment)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition asmCommentDefinition;

        [Export]
        [Name(Constants.classifier_asm_address)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition asmAddressDefinition;

        [Export]
        [Name(Constants.classifier_asm_label)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition asmLabelDefinition;

        [Export]
        [Name(Constants.classifier_asm_locallabel)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition asmLocallabelDefinition;

        [Export]
        [Name(Constants.classifier_asm_cxxdecname)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition asmCxxdecnameDefinition;
    }


    public sealed partial class ClassificationDefinitions
    {
        [Export]
        [Name(Constants.classifier_nsis_instruction)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition nsisInstructionDefinition;

        [Export]
        [Name(Constants.classifier_nsis_intrinsic)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition nsisIntrinsicDefinition;

        [Export]
        [Name(Constants.classifier_nsis_comment)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition nsisCommentDefinition;

        [Export]
        [Name(Constants.classifier_nsis_token)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition nsisTokenDefinition;

        [Export]
        [Name(Constants.classifier_nsis_attribute)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition nsisAttributeDefinition;

        [Export]
        [Name(Constants.classifier_nsis_condition)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition nsisConditionDefinition;

        [Export]
        [Name(Constants.classifier_nsis_string)]
        [BaseDefinition("text")]
        public static ClassificationTypeDefinition nsisStringDefinition;

    }

}
