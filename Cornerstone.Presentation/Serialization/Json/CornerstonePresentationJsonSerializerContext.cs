#region References

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.DockingManager;
using Cornerstone.Presentation.Controls.Text.Folding;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Theme.Theming;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Input;

#endregion

namespace Cornerstone.Presentation.Serialization.Json;

[JsonSerializable(typeof(CollapsedFoldSnapshot))]
[JsonSerializable(typeof(CollapsedFoldSnapshot[]))]
[JsonSerializable(typeof(Dock))]
[JsonSerializable(typeof(DockLayoutItem))]
[JsonSerializable(typeof(List<DockLayoutItem>))]
[JsonSerializable(typeof(Orientation))]
[JsonSerializable(typeof(PresentationList<DockLayoutItem>))]
[JsonSerializable(typeof(ShortcutBinding))]
[JsonSerializable(typeof(SplitFractions))]
[JsonSerializable(typeof(ThemeColor))]
[JsonSerializable(typeof(ThemeDensity))]
public partial class CornerstonePresentationJsonSerializerContext : JsonSerializerContext
{
}