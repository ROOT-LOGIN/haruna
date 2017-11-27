using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace haruna.mutable
{
    public sealed class ClassifierDefinition
    {
        public string classifier;
        public string brief;
        public string usage;        
    }

    public sealed class ClassifierCollection : Dictionary<string, ClassifierDefinition>
    {
        public ClassifierCollection() : base(StringComparer.InvariantCultureIgnoreCase)
        {
            
        }

        public ClassifierCollection(int capacity) : base(capacity, StringComparer.InvariantCultureIgnoreCase)
        {

        }

        public KeyValuePair<string, ClassifierDefinition>[] Classifiers
        {
            get 
            {
                return this.Select(kvp => kvp).ToArray();
            }            
        }
    }

    public sealed class Loader
    {       
        private static Stream GetResourceStream(string resource)
        {
            return typeof(Loader).Assembly.GetManifestResourceStream(Constants.mutable_namespace + resource);
        }

        static string _undnamePath;
        public static string UndnamePath
        {
            get
            {
                if(_undnamePath == null)
                {
                    _undnamePath = AppDomain.CurrentDomain.BaseDirectory;
                    _undnamePath = System.IO.Path.Combine(
                        _undnamePath.Replace(@"Common7\IDE\", string.Empty),
                        @"VC\bin\amd64\undname.exe");
                    if(Environment.Is64BitOperatingSystem == false)
                    {
                        _undnamePath = _undnamePath.Replace("amd64", "x86");
                    }
                }
                return _undnamePath;
            }
        }
        
        public static ClassifierCollection LoadAsmClassifier(AsmClassifierFamilyType type)
        {
            if (type < AsmClassifierFamilyType.General || type > AsmClassifierFamilyType.Masm)
                return null;

            var xml = Constants.classifier_asm_xml;
            ClassifierCollection dict = null;
            LoadAsmClassifier(ref dict, xml);
            if(dict == null || type == AsmClassifierFamilyType.General)
                return dict;
            LoadAsmInstructionUsage(dict);

            xml = Constants.classifier_nasm_xml;
            if(type == AsmClassifierFamilyType.Masm)
                xml = Constants.classifier_masm_xml;
            LoadAsmClassifier(ref dict, xml);
            return dict;
        }

        static readonly Regex s_instrRegex = new Regex("^[ \t\v]*([^ \t\v]+)[ \t\v]", RegexOptions.Compiled);
        private static void LoadAsmInstructionUsage(ClassifierCollection dict)
        {
            var stream = GetResourceStream(Constants.instructions_asm_txt);
            if (stream == null) return;
#if DEBUG
            int m0 = 0, m1 = 0, m2 = 0;
#endif
            using (stream)
            {
                ClassifierDefinition clsdef;
                var doc = new StreamReader(stream);
                while (!doc.EndOfStream)
                {
                    var line = doc.ReadLine();
                    var parts = line.Split(new[] { ' ', '\t', '\v', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0 || !char.IsLetter(parts[0][0])) continue;

                    /*var m = s_instrRegex.Match(line);
                    if (!m.Success) continue;
                    m.Groups[1].Value
                    */
                    string i = null, f = null;
                    if(dict.TryGetValue(parts[0], out clsdef))
                    {
                        m0 = Math.Max(m0, parts[0].Length);
                        
                        if (parts.Length > 2)
                        {
                            i = parts[1];
                            f = parts[2];
#if DEBUG
                            m1 = Math.Max(m1, i.Length);
                            m2 = Math.Max(m2, f.Length);
#endif
                        }
                        else if (parts.Length > 1)
                        {
                            i = string.Empty;
                            f = parts[1];
#if DEBUG
                            m2 = Math.Max(m2, f.Length);
#endif
                        }

                        if (clsdef.usage == null)
                        {
                            clsdef.usage = string.Format("{0} {1} {2}", 
                                parts[0], i, f);
                        }
                        else
                        {
                            clsdef.usage = string.Format("{0}{1}{2} {3} {4}", 
                                clsdef.usage, Environment.NewLine, parts[0], i, f);
                        } 
                    }
                }
#if DEBUG
                Debug.WriteLine("M0: {0} | M1: {1} | M2: {2}", m0, m1, m2);
#endif
            }
        }
       

        private static void LoadAsmClassifier(ref ClassifierCollection dict, string xml)
        {
            var stream = GetResourceStream(xml);
            if(stream == null) return;
            using(stream)
            {
                var xdoc = XDocument.Load(stream);
                if(dict == null) { dict = new ClassifierCollection(); }
                var tydef = Constants.classifier_asm_register;
                LoadAsmClassifier(dict, xdoc, "register", tydef);
                tydef = Constants.classifier_asm_instruction;
                LoadAsmClassifier(dict, xdoc, "instruction", tydef);
                tydef = Constants.classifier_asm_pseudo;
                LoadAsmClassifier(dict, xdoc, "pseudo", tydef);
                tydef = Constants.classifier_asm_intrinsic;
                LoadAsmClassifier(dict, xdoc, "intrinsic", tydef);
            }
        }

        private static void LoadAsmClassifier(ClassifierCollection dict, XDocument xdoc, string elementName, string tydef)
        {
            var collection = xdoc.Root.Element(elementName + "Collection");
            if(collection != null)
            {
                string key = null; 
                foreach(var xe in collection.Elements(elementName))
                {
                    key = xe.Value;
                    var realdef = new ClassifierDefinition() { classifier = tydef };
                    if(xe.HasAttributes) {
                        var attr = xe.Attribute("val");
                        if (attr != null) key = attr.Value;
                        realdef.brief = xe.Value;                      
                    }                    
#if DEBUG
                    TryCatchDictAdd(dict, key, realdef);
#else
                    dict.Add(xe.Value, tydef);
#endif
                }
            }
        }

#if DEBUG
        static void TryCatchDictAdd(ClassifierCollection dict, string key, ClassifierDefinition value)
        {
            try
            {
                dict.Add(key, value);
            }
            catch
            {
                Debugger.Break();
            }
        }
#endif
    }
}
