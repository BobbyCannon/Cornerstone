#region References

using System.ComponentModel;
using Avalonia.Controls;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia;

public class CornerstoneContentControl : ContentControl, IDispatchable
{
    #region Fields

    private PropertyChangedEventHandler _propertyChangedHandler;

    #endregion

    #region Methods

    /// <inheritdoc />
    public IDispatcher GetDispatcher()
    {
        return CornerstoneDispatcher.Instance;
    }

    public static T GetInstance<T>()
    {
        return CornerstoneApplication.GetInstance<T>();
    }

    public void OnPropertyChanged(string propertyName)
    {
        _propertyChangedHandler ??= this.GetPropertyChangedHandler();
        _propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}