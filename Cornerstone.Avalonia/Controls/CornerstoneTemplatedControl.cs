#region References

using System.ComponentModel;
using Avalonia.Controls.Primitives;
using Cornerstone.Presentation;
using PropertyChanged;


#endregion

namespace Cornerstone.Avalonia.Controls;

[DoNotNotify]
public class CornerstoneTemplatedControl : TemplatedControl, IDispatchable
{
   #region Methods

    /// <inheritdoc />
    public IDispatcher GetDispatcher()
    {
        return CornerstoneDispatcher.Instance;
    }

    public static T GetService<T>()
    {
        return CornerstoneApplication.GetService<T>();
    }

   #endregion
}