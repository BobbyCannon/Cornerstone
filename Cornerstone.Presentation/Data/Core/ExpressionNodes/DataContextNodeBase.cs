using System;
using Cornerstone.Presentation.LogicalTree;

namespace Cornerstone.Presentation.Data.Core.ExpressionNodes;

internal abstract class DataContextNodeBase : SourceNode
{
    public override object? SelectSource(object? source, object target, object? anchor)
    {
        if (source != PresentationProperty.UnsetValue)
            throw new NotSupportedException(
                "DataContextNode is invalid in conjunction with a binding source.");
        if (target is IDataContextProvider and PresentationObject)
            return target;
        if (anchor is IDataContextProvider and PresentationObject)
            return anchor;
        throw new InvalidOperationException("Cannot find a DataContext to bind to.");
    }
}
