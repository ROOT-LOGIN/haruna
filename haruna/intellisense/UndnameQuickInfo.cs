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
using System.Windows.Controls;
using System.Windows.Media;
using haruna.adornments;

namespace haruna
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("ToolTip QuickInfo Source")]
    [Order(Before = "Default Quick Info Presenter")]
    [ContentType("text/asm")]    
    public class UndnameQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        [Import]
        internal IClassifierAggregatorService AggregatorFactory;

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {            
            return new UndnameQuickInfoSource(this, textBuffer);
        }
    }

    internal class UndnameQuickInfoSource : IQuickInfoSource
    {
        private UndnameQuickInfoSourceProvider m_provider;
        private ITextBuffer m_textBuffer;
        private IClassifier m_classifier;

        public UndnameQuickInfoSource(UndnameQuickInfoSourceProvider provider, ITextBuffer textBuffer)
        {
            m_provider = provider;
            m_textBuffer = textBuffer;
            m_classifier = GeneralAsmClassifier.GetClassifier(textBuffer);
        }

        #region IQuickInfoSource Members
        bool HasSpecialChar(string str)
        {
            return str.Any(C => IsSpecialChar(C));
        }

        bool IsSpecialChar(char c)
        {
            if(char.IsWhiteSpace(c)) return true;

            switch(c)
            {
                case ';':
                case ',':
                case ':':
                case '[':
                case ']':
                case '(':
                case ')':
                case '{':
                case '}':
                case '"':
                case '\'':
                case '`':
                case '.':
                    return true;
                default:
                    return false;
            }
        }

        static Dictionary<string, string> s_dictUndname = new Dictionary<string, string>();

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            // Map the trigger point down to our buffer.
            SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(m_textBuffer.CurrentSnapshot);
            if(!subjectTriggerPoint.HasValue)
            {
                return;
            }

            ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
            SnapshotSpan querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);
            //look for occurrences of our QuickInfo words in the span
            ITextStructureNavigator navigator = m_provider.NavigatorService.GetTextStructureNavigator(m_textBuffer);            
            TextExtent extent = navigator.GetExtentOfWord(subjectTriggerPoint.Value);
            var spans = m_classifier.GetClassificationSpans(extent.Span);
            var textView = session.TextView as IWpfTextView;
            SimpleAdroner box = null;
            if (textView != null)
            {
                if (textView.Properties.TryGetProperty(typeof(SimpleAdroner), out box))
                {
                    box.CleanUp();
                }
            }
            if (box != null && spans.Count != 0)
            {
                var spobj = (HurunaClassificationSpan)spans.FirstOrDefault(sp => sp is HurunaClassificationSpan);
                if(spobj != null && spobj.Definition != null)
                {
                    applicableToSpan = currentSnapshot.CreateTrackingSpan(                    
                        querySpan.Start.Position, querySpan.Length, SpanTrackingMode.EdgeExclusive);
                    if (!string.IsNullOrEmpty(spobj.Definition.brief))
                        qiContent.Add(spobj.Definition.brief);
                    
                    if (!string.IsNullOrEmpty(spobj.Definition.usage))
                        box.SetText(spobj.Definition.usage);

                    // Debug.WriteLine("QuickInfo: {0}", new[] { spobj.Span.GetText() });
                    return;
                }
            }

            if (HasSpecialChar(extent.Span.GetText()))
            {
                return;
            }
            var t = subjectTriggerPoint.Value.GetContainingLine();
            var line = t.GetText();
            if(string.IsNullOrEmpty(line)) return;

            int i = subjectTriggerPoint.Value.Position;
            for(; i >= t.Start.Position; i--)
            {
                if(IsSpecialChar(line[i - t.Start.Position]))
                {
                    break;
                }
            }
            if(i < t.Start.Position) i = t.Start.Position;
            else i++;
            int j = subjectTriggerPoint.Value.Position;            
            for(; j <= t.End.Position; j++)
            {
                if(j == t.End.Position || IsSpecialChar(line[j - t.Start.Position]))
                {
                    break;
                }
            }
            if(j > t.End.Position) j = t.End.Position;
            
            var k = line.Substring(i - t.Start.Position, j - i);
            if(char.IsNumber(k[0]))
            {
                ulong u;
                if(ParseULong(k, out u))
                {
                    applicableToSpan = currentSnapshot.CreateTrackingSpan(
                                        querySpan.Start.Position, querySpan.Length, SpanTrackingMode.EdgeInclusive);                                                       
                    qiContent.Add(string.Format("DEC: {0:D}\nHEX: {0:X}", u));
                }
            }
            else if(k[0] == '?')
            {
                string str;
                if(s_dictUndname.TryGetValue(k, out str) == false)
                {
                    Process ps = new Process();
                    ps.StartInfo = new ProcessStartInfo()
                    {
                        FileName = mutable.Loader.UndnamePath,
                        Arguments = k,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    ps.Start();
                    str = ps.StandardOutput.ReadLine();
                    while(str != null)
                    {
                        if(str.StartsWith("is :- "))
                        {
                            str = str.Substring(7).Trim(' ', '"');
                            s_dictUndname[k] = str;
                            break;
                        }
                        str = ps.StandardOutput.ReadLine();
                    }
                }

                applicableToSpan = currentSnapshot.CreateTrackingSpan(
                    querySpan.Start.Position, querySpan.Length, SpanTrackingMode.EdgeInclusive);
                qiContent.Add("UNDECORATED TO");
                qiContent.Add(str); // the content can be string or WPF element.
            }
            else if(i != t.Start)
            {
                if(line[i - t.Start.Position - 1] == '.')
                {
                    NasmClassifier nasmclassifier;                    
                    if (m_textBuffer.Properties.TryGetProperty(typeof(NasmClassifier), out nasmclassifier))
                    {
                        var lbs = nasmclassifier.GetLabels();
                        if (lbs.Count != 0)
                        {
                            int? z = lbs.Keys.Where(y => y <= t.LineNumber).Max(p => (int?)p);
                            if (z.HasValue && lbs.ContainsKey(z.Value))
                            {
                                applicableToSpan = currentSnapshot.CreateTrackingSpan(
                                    querySpan.Start.Position, querySpan.Length, SpanTrackingMode.EdgeInclusive);
                                qiContent.Add("LOCAL LABEL FOR");
                                qiContent.Add(string.Format("{0}\nAT LINE #{1}", lbs[z.Value], z.Value + 1));
                            }
                        }
                    }
                }
            }
        }
        #endregion

        bool ParseULong(string s, out ulong value)
        {
            var spec = System.Globalization.NumberStyles.None;
            s = s.ToUpper();
            if(s[s.Length - 1] == 'h' || s[s.Length - 1] == 'H')
            {
                spec = System.Globalization.NumberStyles.AllowHexSpecifier;
                s = s.Substring(0, s.Length-1);
            }

            else if(s.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            {
                spec = System.Globalization.NumberStyles.AllowHexSpecifier;
                s = s.Substring(2);
            }

            else if(s.StartsWith("00", StringComparison.InvariantCultureIgnoreCase))
            {
                spec = System.Globalization.NumberStyles.AllowHexSpecifier;
            }

            else if(s.Any(c=>c == 'A' || c == 'B' || c == 'C' || c == 'D' || c == 'E' || c == 'F'))
            {
                spec = System.Globalization.NumberStyles.AllowHexSpecifier;
            }

            return ulong.TryParse(s, spec, null, out value);
        }
        #region IDisposable Members

        private bool m_isDisposed;
        public void Dispose( )
        {
            if(!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
        #endregion
    }

    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("ToolTip QuickInfo Controller")]
    [ContentType("text/asm")]
    //[FileExtension(".asm")]
    public class UndnameQuickInfoControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        internal IQuickInfoBroker QuickInfoBroker { get; set; }
        
        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new UndnameQuickInfoController(textView, subjectBuffers, this);
        }    
    }

    internal class UndnameQuickInfoController : IIntellisenseController
    {
        private ITextView m_textView;
        private IList<ITextBuffer> m_subjectBuffers;
        private UndnameQuickInfoControllerProvider m_provider;
        private IQuickInfoSession m_session;

        internal UndnameQuickInfoController(ITextView textView, IList<ITextBuffer> subjectBuffers, UndnameQuickInfoControllerProvider provider)
        {
            m_textView = textView;
            m_subjectBuffers = subjectBuffers;
            m_provider = provider;

            m_textView.MouseHover += this.OnTextViewMouseHover;
        }

        private void OnTextViewMouseHover(object sender, MouseHoverEventArgs e)
        {
            //find the mouse position by mapping down to the subject buffer
            SnapshotPoint? point = m_textView.BufferGraph.MapDownToFirstMatch
                 (new SnapshotPoint(m_textView.TextSnapshot, e.Position),
                PointTrackingMode.Positive,
                snapshot => m_subjectBuffers.Contains(snapshot.TextBuffer),
                PositionAffinity.Predecessor);

            if(point != null)
            {
                ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position,
                PointTrackingMode.Positive);

                if(!m_provider.QuickInfoBroker.IsQuickInfoActive(m_textView))
                {
                    m_session = m_provider.QuickInfoBroker.TriggerQuickInfo(m_textView, triggerPoint, true);
                }
            }
        }

        public void Detach(ITextView textView)
        {
            if(m_textView == textView)
            {
                m_textView.MouseHover -= this.OnTextViewMouseHover;
                m_textView = null;
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
        }
    }
}
