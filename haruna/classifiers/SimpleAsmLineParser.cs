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
    public class SimpleAsmLineParser
    {
        internal static readonly ClassifierCollection[] s_ClassifierDictionary;
                
        static SimpleAsmLineParser()
        {
            s_ClassifierDictionary = new ClassifierCollection[3];
        }

        public static int SkipSpace(string line, int start)
        {
            if(line == null) throw new ArgumentNullException("line");
            if(start < 0) throw new ArgumentOutOfRangeException("start");

            if(line.Length == 0) return 0;

            if(start - line.Length >= 0) return line.Length;

            while(start < line.Length && char.IsWhiteSpace(line[start])) start++;
            return start;
        }

        public delegate void ParseCallback(ClassifierDefinition definition, string parseType, int start, int length);

        public readonly ClassifierCollection m_ClassifierDictionary;
        public readonly AsmClassifierFamilyType m_ClassifierFamily;

        public SimpleAsmLineParser(AsmClassifierFamilyType classifierFamily)
        {
            m_ClassifierFamily = classifierFamily;
            if(s_ClassifierDictionary[(int)classifierFamily] == null)
            {
                s_ClassifierDictionary[(int)classifierFamily] =
                    mutable.Loader.LoadAsmClassifier(classifierFamily);
            }
            m_ClassifierDictionary = s_ClassifierDictionary[(int)classifierFamily];
        }

        public void Parse(string text, ParseCallback callback)
        {
            int baseoff = 0;
            foreach(var line in text.Split('\n'))
            {
                ParseLine(baseoff, line, callback);
                baseoff += line.Length + 1;
            }
        }

        void ParseLine(int baseoff, string line, ParseCallback callback)
        {
            if(callback == null || string.IsNullOrEmpty(line)) 
                return;
            
            int s = SkipSpace(line, 0);
            int e = line.IndexOf(";", s);
            if(e < 0) // no comment
            {
                ParseLabel(baseoff, s, line.Trim(), callback);
            }
            else if(e == 0) // only comment
            {
                callback(null, Constants.classifier_asm_comment, baseoff + s, line.Length);
            }
            else // comment and other
            {
                ParseLabel(baseoff, s, line.Substring(s, e - s).Trim(), callback);
                callback(null, Constants.classifier_asm_comment, baseoff + e, line.Length - e);
            }            
        }
        
        void ParseLabel(int baseoff, int offset, string line, ParseCallback callback)
        {
            if(string.IsNullOrEmpty(line)) return;

            int s = 0;
            int e = -1;
            while(true)
            {
                e = line.IndexOf(':', s);
                if(e < 0) // no more label
                {
                    ParseAddress(baseoff, offset + s, line.Substring(s).Trim(), callback);
                    break;
                }
                else if(e == 0) // empty label, check next
                {
                    s = SkipSpace(line, e + 1);
                    continue;
                }
                else // found a label, check next
                {
                    var xl = line.Substring(s, e - s);
                    // if has any special char, it's not an effective label
                    if(xl.Any(c => IsSpecialChar(c)))
                    {                        
                        ParseAddress(baseoff, offset + s, xl.Trim(), callback);                        
                    }
                    else
                    {
                        // the label may a segment register
                        callback(null,
                            IsSegmentRegister(line.Substring(s, e - s)) ? Constants.classifier_asm_register : Constants.classifier_asm_label,
                            baseoff + offset + s, e - s);                        
                    }
                    s = SkipSpace(line, e + 1);
                }
                
                if(s >= line.Length) // no more word
                    return;
            }
        }

        void ParseAddress(int baseoff, int offset, string line, ParseCallback callback)
        {
            int s = 0;
            int e = 0;
            while(true)
            {
                e = line.IndexOf('[', s);
                if(e < 0) // no address
                {
                    ParseInstruction(baseoff, offset + s, line.Substring(s).Trim(), callback);
                    break;
                }
                else if(e == 0) // a address, try match ]
                {
                    s = e;
                    e = line.IndexOf(']', s);
                    if(e < s) // no match
                    {
                        break; // treat as text
                    }
                    else
                    {
                        e++;
                        callback(null, Constants.classifier_asm_address, baseoff + offset + s, e - s);
                        s = SkipSpace(line, e);
                    }
                }
                else // find address, try match ]
                {                    
                    ParseInstruction(baseoff, offset + s, line.Substring(s, e - s).Trim(), callback);
                    s = e;
                    e = line.IndexOf(']', s);
                    if(e < s) // no match
                    {
                        break; //treat as text
                    }
                    else
                    {
                        e++;
                        callback(null, Constants.classifier_asm_address, baseoff + offset + s, e - s);                        
                        s = SkipSpace(line, e);
                    }
                }

                if(e >= line.Length)
                    break;
            }
        }
        
        void ParseInstruction(int baseoff, int offset, string line, ParseCallback callback)
        {
            int i = 0;
            var parts = line.Split(new[] { ' ', '\t', ',', '+', '-', '*', ':' }, StringSplitOptions.None);
            ClassifierDefinition definition = null;
            foreach (var part in parts)
            {
                if(part.StartsWith("?"))
                {
                    callback(null, Constants.classifier_asm_cxxdecname, baseoff + offset + i, part.Trim().Length);
                }
                else if (m_ClassifierDictionary.TryGetValue(part, out definition))
                {
                    callback(definition, definition.classifier, baseoff + offset + i, part.Length);                    
                }
                i += part.Length;
                i++;
            }
        }

        bool IsSpecialChar(char c)
        {
            switch(c)
            {
                case ';':
                case ',':
                case '%':
                case '#':
                case '(':
                case ')':
                case '*':
                case '[':
                case ']':
                case '<':
                case '>':
                    return true;                    
            }
            return char.IsWhiteSpace(c);
        }

        static bool IsSegmentRegister(string line)
        {
            switch(line.ToLower())
            {
                case "cs":
                case "ds":
                case "es":
                case "ss":
                case "fs":
                case "gs": 
                    return true;
            }
            return false;
        }
    }
}
