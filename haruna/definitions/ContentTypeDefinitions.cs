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
    public sealed partial class ContentTypeDefinitions
    {
        [Export]
        [Name("text/asm")]
        [BaseDefinition("text")]
        public static ContentTypeDefinition asmDefinition;
        
        [Export]
        [Name("text/nasm")]
        [BaseDefinition("text/asm")]
        public static ContentTypeDefinition nasmDefinition;
        
        [Export]
        [Name("text/masm")]
        [BaseDefinition("text/asm")]
        public static ContentTypeDefinition masmDefinition;

        [Export]
        [FileExtension(".asm")]        
        [ContentType("text/asm")]
        public static FileExtensionToContentTypeDefinition asmFileDefinition;

        [Export]
        [FileExtension(".nasm")]
        [ContentType("text/nasm")]
        public static FileExtensionToContentTypeDefinition nasmFileDefinition;

        [Export]
        [FileExtension(".masm")]
        [ContentType("text/masm")]
        public static FileExtensionToContentTypeDefinition masmFileDefinition;
    }


    public sealed partial class ContentTypeDefinitions
    {
        [Export]
        [Name("text/nsis")]
        [BaseDefinition("text")]
        public static ContentTypeDefinition nsisDefinition;

        [Export]
        [Name("text/nsi")]
        [BaseDefinition("text/nsis")]
        public static ContentTypeDefinition nsiDefinition;

        [Export]
        [Name("text/nsh")]
        [BaseDefinition("text/nsis")]
        public static ContentTypeDefinition nshDefinition;

        [Export]
        [Name("text/nlf")]
        [BaseDefinition("text/nsis")]
        public static ContentTypeDefinition nlfDefinition;

        [Export]
        [FileExtension(".nsi")]
        [ContentType("text/nsi")]
        public static FileExtensionToContentTypeDefinition nsiFileDefinition;

        [Export]
        [FileExtension(".nsh")]
        [ContentType("text/nsh")]
        public static FileExtensionToContentTypeDefinition nshFileDefinition;

        [Export]
        [FileExtension(".nlf")]
        [ContentType("text/nlf")]
        public static FileExtensionToContentTypeDefinition nlfFileDefinition;
    }

}
