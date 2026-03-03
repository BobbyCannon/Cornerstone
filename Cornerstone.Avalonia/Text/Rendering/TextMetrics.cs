#region References

using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.Text.Rendering;

public partial class TextMetrics : Notifiable
{
	#region Properties

	[Notify]
	public partial double CharacterHeight { get; set; }

	[Notify]
	public partial double CharacterWidth { get; set; }

	#endregion
}