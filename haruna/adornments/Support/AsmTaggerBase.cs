//***************************************************************************
//
//    Copyright (c) Microsoft Corporation. All rights reserved.
//    This code is licensed under the Visual Studio SDK license terms.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//***************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace haruna
{
    /// <summary>
    /// Helper base class for writing simple taggers based on regular expressions.
    /// </summary>
    /// <remarks>
    /// Regular expressions are expected to be single-line.
    /// </remarks>
    /// <typeparam name="T">The type of tags that will be produced by this tagger.</typeparam>
    public abstract class AsmTaggerBase<T> : ITagger<T> where T : ITag
    {
        public AsmTaggerBase(ITextBuffer buffer)
        {
            buffer.Changed += (sender, args) => HandleBufferChanged(args);

        }

        #region ITagger implementation

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion

        /// <summary>
        /// Handle buffer changes. The default implementation expands changes to full lines and sends out
        /// a <see cref="TagsChanged"/> event for these lines.
        /// </summary>
        /// <param name="args">The buffer change arguments.</param>
        protected virtual void HandleBufferChanged(TextContentChangedEventArgs args)
        {
            if (args.Changes.Count == 0)
                return;

            if (TagsChanged == null)
                return;

            // Combine all changes into a single span so that
            // the ITagger<>.TagsChanged event can be raised just once for a compound edit
            // with many parts.

            ITextSnapshot snapshot = args.After;

            int start = args.Changes[0].NewPosition;
            int end = args.Changes[args.Changes.Count - 1].NewEnd;

            SnapshotSpan totalAffectedSpan = new SnapshotSpan(
                snapshot.GetLineFromPosition(start).Start,
                snapshot.GetLineFromPosition(end).End);

            TagsChanged(this, new SnapshotSpanEventArgs(totalAffectedSpan));
        }

        public abstract IEnumerable<ITagSpan<T>> GetTags(NormalizedSnapshotSpanCollection spans);
    }
}
