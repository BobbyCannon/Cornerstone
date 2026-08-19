namespace Cornerstone.GrokMonitor.GrokUsage.Models;

/// <summary>
/// Session-row heat: theme step plus alpha (UI-free).
/// </summary>
public readonly struct TokenHeatColor
{
	#region Fields

	/// <summary>
	/// Transparent / no heat.
	/// </summary>
	public static readonly TokenHeatColor None = new(0, 0);

	#endregion

	#region Constructors

	/// <summary>
	/// Creates heat channels.
	/// </summary>
	public TokenHeatColor(byte a, int themeIndex)
	{
		A = a;
		ThemeIndex = themeIndex;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Alpha 0–255.
	/// </summary>
	public byte A { get; }

	/// <summary>
	/// True when the tint is fully transparent.
	/// </summary>
	public bool IsNone => A == 0;

	/// <summary>
	/// Theme shade index 0–9 for ThemeColor00–ThemeColor09.
	/// </summary>
	public int ThemeIndex { get; }

	#endregion
}