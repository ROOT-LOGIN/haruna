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
    enum RegionType
    {
        RT_UNDEFINED = 0,

        RT_Comment,
        RT_Macro,
        RT_Struct,
        RT_Condition,
        RT_Segment,
        RT_Procedure
    }

    class PartialRegion
    {
        public RegionType Type { get; set; }
        public int StartLine { get; set; }
        public int StartOffset { get; set; }
        public int Level { get; set; }
        public PartialRegion PartialParent { get; set; }
    }

    class Region : PartialRegion
    {
        public Region( ) { EndLine = -1; }

        public int EndLine { get; set; }
    }

    internal class HurunaHelper
    {
        public const string Ellipsis = "......";

        public static string GetHoverText(ITextSnapshot currentSnapshot, Region region)
        {
            string hoverText =string.Empty;
            // read first 6 lines as hover text
            int max = 5;

            for(int i = region.StartLine + 1; i < region.EndLine; i++)
            {
                var line = currentSnapshot.GetLineFromLineNumber(i);
                var linetxt = line.GetTextIncludingLineBreak().TrimStart();
                if(string.IsNullOrEmpty(linetxt)) linetxt = Environment.NewLine;
                hoverText += linetxt;
                if(i - region.StartLine > max)
                {
                    break;
                }
            }
            hoverText += Ellipsis;
            return hoverText;
        }

        public static bool GetCxxUndecName(string arg, out string value)
        {
            Process ps = new Process();
            ps.StartInfo = new ProcessStartInfo()
            {
                FileName = mutable.Loader.UndnamePath,
                Arguments = arg,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            ps.Start();
            value = ps.StandardOutput.ReadLine();
            while (value != null)
            {
                if (value.StartsWith("is :- "))
                {
                    value = value.Substring(7).Trim(' ', '"');
                    return true;
                }
                value = ps.StandardOutput.ReadLine();
            }
            return false;
        }
    }

}
