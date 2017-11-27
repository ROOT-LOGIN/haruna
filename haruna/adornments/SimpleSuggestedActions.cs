using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using System.Threading;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Imaging.Interop;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Classification;
using System.Windows.Controls;
using System.Windows;

namespace haruna
{  
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Suggested Actions Source")]
    [ContentType("text/asm")]
    public class SimpleSuggestedActionsProvider : ISuggestedActionsSourceProvider
    {
        [Import(typeof(ITextStructureNavigatorSelectorService))]
        internal ITextStructureNavigatorSelectorService navigatorService;
        
        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            return new SimpleSuggestedActions(this, textView, textBuffer);
        }
    }

    internal class SimpleSuggestedActions : ISuggestedActionsSource
    {
        SimpleSuggestedActionsProvider m_provider;
        ITextView m_textView;
        ITextBuffer m_textBuffer;
        IClassifier m_classifier;

        public SimpleSuggestedActions(SimpleSuggestedActionsProvider provider, ITextView textView, ITextBuffer textBuffer)
        {
            m_provider = provider;
            m_textView = textView;
            m_textBuffer = textBuffer;
            m_classifier = GeneralAsmClassifier.GetClassifier(textBuffer);
        }

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        public void Dispose()
        {
            
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {            
            return new SuggestedActionSet[1] {
                new SuggestedActionSet(m_classifier.GetClassificationSpans(range).Where(c=>
                {
                    if (c.ClassificationType.Classification == Constants.classifier_asm_cxxdecname) return true;
                    if (c.ClassificationType.Classification == Constants.classifier_asm_label && c.Span.GetText().StartsWith("?")) return true;
                    return false;
                }).Select(c=>new CopyCxxUndnameSuggestedAction(c.Span)))
            };
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => {                
                var b = range.GetText().ToLower().Contains("?");
                if(b) SuggestedActionsChanged(this, EventArgs.Empty);
                return b;
            });
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }

    internal class CopyCxxUndnameSuggestedAction : ISuggestedAction
    {
        SnapshotSpan m_span;
        public CopyCxxUndnameSuggestedAction(SnapshotSpan span)
        {
            m_span = span;
        }
        public string DisplayText
        {
            get
            {
                var txt = m_span.GetText().Trim();
                if(txt.Length > 10)
                {
                    return string.Format("Undecorated To clipboard [{0} ...]", txt.Substring(0, 10));
                }
                else
                {
                    return string.Format("Undecorated To clipboard [{0}]", txt);
                }
                
            }
        }

        public bool HasActionSets
        {
            get
            {
                return false;
            }
        }

        public bool HasPreview
        {
            get
            {
                return true;
            }
        }

        public string IconAutomationText
        {
            get
            {
                return string.Empty;
            }
        }

        public ImageMoniker IconMoniker
        {
            get
            {
                return new ImageMoniker();
            }
        }

        public string InputGestureText
        {
            get
            {
                return string.Empty;
            }
        }

        public void Dispose()
        {

        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return new Task<IEnumerable<SuggestedActionSet>>(() => null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            var textBlock = new TextBlock();
            textBlock.Padding = new Thickness(5);
            string txt;
            if(!HurunaHelper.GetCxxUndecName(m_span.GetText().Trim(), out txt))
            {
                txt = "[Invaid Input]";
            }
            textBlock.Text = txt; 
            return Task.FromResult<object>(textBlock);            
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            string txt;
            if (HurunaHelper.GetCxxUndecName(m_span.GetText().Trim(), out txt))
            {
                Clipboard.SetData(DataFormats.UnicodeText, txt);
            }
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }

}
