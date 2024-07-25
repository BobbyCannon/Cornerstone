#region References

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Shared;

public interface IAvaloniaHandlerCollection : IList<Type>, ICollection<Type>, IEnumerable<Type>, IEnumerable
{
}