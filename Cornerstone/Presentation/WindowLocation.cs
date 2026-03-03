#region References

using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Serialization;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a window location.
/// </summary>
[SourceReflection]
public partial class WindowLocation : Notifiable, IPackable
{
	#region Constructors

	public WindowLocation()
	{
		ResetToDefaults();
	}

	#endregion

	#region Properties

	[Pack(1, 1)]
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Height { get; set; }

	[Pack(1, 2)]
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Left { get; set; }

	[Pack(1, 3)]
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool Maximized { get; set; }

	[Pack(1, 4)]
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Top { get; set; }

	[Pack(1, 5)]
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Width { get; set; }

	#endregion

	#region Methods

	public bool IsDefaultLocation()
	{
		return !Maximized
			&& (Left == -1)
			&& (Top == -1);
	}

	public void ResetToDefaults()
	{
		Height = 600;
		Left = -1;
		Top = -1;
		Maximized = false;
		Width = 1024;
	}

	public override string ToString()
	{
		return $"{Left}x{Top} {Height}x{Width} {Maximized}";
	}

	#endregion
}