/* This File Is Not Included In Build Process */

using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Formatting;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text;

// IGlyphMouseProcessorProvider
namespace haruna
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("SimpleGlyph")]
    [Order(After = "VsTextMarker")]
    //[Order(Before = "Wpf Vertical Scrollbar")]
    //[MarginContainer(PredefinedMarginNames.VerticalScrollBarContainer)]
    [ContentType("text/asm")]
    [TagType(typeof(SimpleGlyphTag))]
    internal sealed class SimpleGlyphFactoryProvider : IGlyphFactoryProvider
    {
        /// <summary> 
        /// This method creates an instance of our custom glyph factory for a given text view. 
        /// </summary> 
        /// <param name="view">The text view we are creating a glyph factory for, we don't use this.</param> 
        /// <param name="margin">The glyph margin for the text view, we don't use this.</param> 
        /// <returns>An instance of our custom glyph factory.</returns> 
        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            return new SimpleGlyphFactory();
        }
    }

    internal class SimpleGlyphFactory : IGlyphFactory
    {
        public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {
            return new Button()
            {
                Width = 16,
                Height = 16,
                Content = 1
            };
        }
    }

    internal class SimpleGlyphTag : IGlyphTag { }

    /// <summary> 
    /// This class implements ITagger for ToDoTag.  It is responsible for creating 
    /// ToDoTag TagSpans, which our GlyphFactory will then create glyphs for. 
    /// </summary> 
    internal class SimpleGlyphTagTagger : ITagger<SimpleGlyphTag>
    {
        #region Private members 
        private IClassifier _aggregator;
        private const string _searchText = "todo";
        #endregion

        #region Constructors 
        internal SimpleGlyphTagTagger(IClassifier aggregator)
        {
            _aggregator = aggregator;
        }
        #endregion

        #region ITagger<ToDoTag> Members 

        /// <summary> 
        /// This method creates ToDoTag TagSpans over a set of SnapshotSpans. 
        /// </summary> 
        /// <param name="spans">A set of spans we want to get tags for.</param> 
        /// <returns>The list of ToDoTag TagSpans.</returns> 
        IEnumerable<ITagSpan<SimpleGlyphTag>> ITagger<SimpleGlyphTag>.GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (SnapshotSpan span in spans)
            {
                // Look at each classification span inside the requested span 
                foreach (ClassificationSpan classification in _aggregator.GetClassificationSpans(span))
                {
                    // If the classification is a comment... 
                    if (classification.ClassificationType.Classification == Constants.classifier_asm_cxxdecname)
                    {
                        // Look for the word "todo" in the comment.  If it is found then create a new ToDoTag TagSpan 
                        //int index = classification.Span.GetText().ToLower().IndexOf(_searchText);
                        yield return new TagSpan<SimpleGlyphTag>(
                            new SnapshotSpan(classification.Span.Start, classification.Span.Length), new SimpleGlyphTag());
                    }
                }
            }
        }

#pragma warning disable 67
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 67
        #endregion
    }

    [Export(typeof(ITaggerProvider))]
    [ContentType("text/asm")]
    [TagType(typeof(SimpleGlyphTag))]
    class ToDoTaggerProvider : ITaggerProvider
    {
        [Import]
        internal IClassifierAggregatorService AggregatorFactory;

        /// <summary> 
        /// Creates an instance of our custom TodoTagger for a given buffer. 
        /// </summary> 
        /// <typeparam name="T"></typeparam> 
        /// <param name="buffer">The buffer we are creating the tagger for.</param> 
        /// <returns>An instance of our custom TodoTagger.</returns> 
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            return new SimpleGlyphTagTagger(AggregatorFactory.GetClassifier(buffer)) as ITagger<T>;
        }
    }
}
