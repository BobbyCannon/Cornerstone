using System;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Diagnostics;

namespace Cornerstone.Presentation.Data
{
    internal class IndexerBinding : BindingBase
    {
        public IndexerBinding(
            PresentationObject source,
            PresentationProperty property,
            BindingMode mode)
        {
            Source = source;
            Property = property;
            Mode = mode;
        }

        public PresentationProperty Property { get; }

        private PresentationObject Source { get; }
        private BindingMode Mode { get; }

        internal override BindingExpressionBase CreateInstance(
            PresentationObject target,
            PresentationProperty? targetProperty,
            object? anchor)
        {
            return new IndexerBindingExpression(Source, Property, target, targetProperty, Mode);
        }
    }
}
