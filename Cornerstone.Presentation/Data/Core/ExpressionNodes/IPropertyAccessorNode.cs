using Cornerstone.Presentation.Data.Core.Plugins;

namespace Cornerstone.Presentation.Data.Core.ExpressionNodes;

/// <summary>
/// Indicates that a <see cref="ExpressionNode"/> accesses a property on an object.
/// </summary>
internal interface IPropertyAccessorNode
{
    string PropertyName { get; }
    IPropertyAccessor? Accessor { get; }
    void EnableDataValidation();
}
