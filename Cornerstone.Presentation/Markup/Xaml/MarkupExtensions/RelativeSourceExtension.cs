using System;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Markup.Xaml.MarkupExtensions
{
    public class RelativeSourceExtension
    {
        public RelativeSourceExtension()
        {
        }

        public RelativeSourceExtension(RelativeSourceMode mode)
        {
            Mode = mode;
        }

        public RelativeSource ProvideValue(IServiceProvider serviceProvider)
        {
            return new RelativeSource
            {
                Mode = Mode,
                AncestorType = AncestorType,
                AncestorLevel = AncestorLevel,
                Tree = Tree,
            };
        }

        [ConstructorArgument("mode")]
        public RelativeSourceMode Mode { get; set; } = RelativeSourceMode.FindAncestor;

        public Type? AncestorType { get; set; }

        public TreeType Tree { get; set; }

        public int AncestorLevel { get; set; } = 1;
    }
}
