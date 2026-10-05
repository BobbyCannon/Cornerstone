using System;

namespace Cornerstone.Presentation.Data.Core.ExpressionNodes;

internal sealed class DataContextNode : DataContextNodeBase
{
    protected override void OnSourceChanged(object? source, Exception? dataValidationError)
    {
        if (!ValidateNonNullSource(source))
            return;

        if (source is IDataContextProvider && source is PresentationObject ao)
        {
            ao.PropertyChanged += OnPropertyChanged;
            SetValue(ao.GetValue(StyledElement.DataContextProperty));
        }
        else
        {
            SetError($"Unable to read DataContext from '{source.GetType()}'.");
        }
    }

    protected override void Unsubscribe(object oldSource)
    {
        if (oldSource is StyledElement oldElement)
            oldElement.PropertyChanged -= OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PresentationPropertyChangedEventArgs e)
    {
        if (sender == Source && e.Property == StyledElement.DataContextProperty)
            SetValue(e.NewValue);
    }
}
