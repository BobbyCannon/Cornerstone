#region References

using Avalonia.Controls;
using Avalonia.Layout;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.DockingManager;
using Cornerstone.Presentation;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cornerstone.Avalonia.Themes;

#endregion

namespace Cornerstone.Avalonia.Serialization.Json;

[JsonSerializable(typeof(Dock))]
[JsonSerializable(typeof(DockLayoutItem))]
[JsonSerializable(typeof(List<DockLayoutItem>))]
[JsonSerializable(typeof(Orientation))]
[JsonSerializable(typeof(PresentationList<DockLayoutItem>))]
[JsonSerializable(typeof(ShortcutBinding))]
[JsonSerializable(typeof(SplitFractions))]
[JsonSerializable(typeof(ThemeColor))]
[JsonSerializable(typeof(ThemeDensity))]
public partial class CornerstoneAvaloniaJsonSerializerContext : JsonSerializerContext
{
}