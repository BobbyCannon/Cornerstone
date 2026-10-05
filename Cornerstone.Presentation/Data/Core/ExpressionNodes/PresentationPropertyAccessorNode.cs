using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Presentation.Utilities;

namespace Cornerstone.Presentation.Data.Core.ExpressionNodes;

internal sealed class PresentationPropertyAccessorNode :
    ExpressionNode,
    ISettableNode,
    IWeakEventSubscriber<PresentationPropertyChangedEventArgs>
{
    private readonly bool _acceptsNull;

    public PresentationPropertyAccessorNode(PresentationProperty property, bool acceptsNull)
    {
        Property = property;
        _acceptsNull = acceptsNull;
    }

    public PresentationProperty Property { get; }
    public Type? ValueType => Property.PropertyType;

    override public void BuildString(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[builder.Length - 1] != '!')
            builder.Append('.');
        builder.Append(Property.Name);
    }

    public bool WriteValueToSource(object? value, IReadOnlyList<ExpressionNode> nodes)
    {
        if (Source is PresentationObject o)
        {
            o.SetValue(Property, value);
            return true;
        }

        return false;
    }

    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (source is null)
        {
            if (_acceptsNull)
                ShortCircuitNull();
            else
                ValidateNonNullSource(source);
            return;
        }

        if (source is PresentationObject newObject)
        {
            WeakEvents.PresentationPropertyChanged.Subscribe(newObject, this);
            SetValue(newObject.GetValue(Property));
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        if (oldSource is PresentationObject oldObject)
            WeakEvents.PresentationPropertyChanged.Unsubscribe(oldObject, this);
    }

    public void OnEvent(object? sender, WeakEvent ev, PresentationPropertyChangedEventArgs e)
    {
        if (e.Property == Property && Source is PresentationObject o)
            SetValue(o.GetValue(Property));
    }
}
