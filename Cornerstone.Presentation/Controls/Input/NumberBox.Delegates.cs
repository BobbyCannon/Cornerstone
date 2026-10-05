using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;

namespace Cornerstone.Presentation.Controls.Input;

/// <summary>
/// EventHandler delegate with an explicit Type
/// </summary>
public delegate void TypedEventHandler<TSender, TResult>(TSender sender, TResult args);

/// <summary>
/// Event handler for selection changed events
/// </summary>
public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs args);