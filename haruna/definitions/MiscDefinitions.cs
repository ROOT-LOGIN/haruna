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
    public sealed partial class MiscDefinitions
    {
        /*[Export]
        [Name(PredefinedSuggestedActionCategoryNames.Any)]
        [BaseDefinition(PredefinedSuggestedActionCategoryNames.Any)]    // zero or more BaseDefinitions are allowed
        internal SuggestedActionCategoryDefinition errorFixSuggestedActionCategoryDefinition;*/

        [Export]
        [Name("UiAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Text)]
        internal AdornmentLayerDefinition uiLayerDefinition;
    }
}
