using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace haruna
{
    public enum AsmClassifierFamilyType 
    { 
        General, Nasm, Masm 
    };

    public sealed class Constants
    {
        public const string mutable_namespace = "haruna.mutable.";

        public const string classifier_asm_xml = "classifier.asm.xml";
        public const string classifier_nasm_xml = "classifier.nasm.xml";
        public const string classifier_masm_xml = "classifier.masm.xml";
        public const string instructions_asm_txt = "instructions.asm.txt";

        public const string classifier_asm_register = "asm/register";
        public const string classifier_asm_pseudo = "asm/pseudo";
        public const string classifier_asm_intrinsic = "asm/intrinsic";
        public const string classifier_asm_instruction = "asm/instruction";
        public const string classifier_asm_comment = "asm/comment";
        public const string classifier_asm_address = "asm/address";
        public const string classifier_asm_label = "asm/label";
        public const string classifier_asm_locallabel = "asm/locallabel";

        public const string classifier_asm_cxxdecname = "asm/cxxdecname";

        // ====================================================================

        public const string classifier_nsis_instruction = "nsis/instruction";
        public const string classifier_nsis_intrinsic = "nsis/intrinsic";
        public const string classifier_nsis_comment = "nsis/comment";
        public const string classifier_nsis_token = "nsis/token";
        public const string classifier_nsis_attribute = "nsis/attribute";
        public const string classifier_nsis_condition = "nsis/condition";
        public const string classifier_nsis_string = "nsis/string";
    }
}
