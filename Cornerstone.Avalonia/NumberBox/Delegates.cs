#region References

using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.NumberBox;

/// <summary>
/// EventHandler delegate with an explicit Type
/// </summary>
public delegate void TypedEventHandler<TSender, TResult>(TSender sender, TResult args);

/// <summary>
/// Event handler for selection changed events
/// </summary>
public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs args);