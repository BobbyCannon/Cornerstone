namespace Cornerstone.GrokMonitor.GrokUsage.Models;

/// <summary>
/// ARGB channels for session-row heat tint (UI-free).
/// </summary>
public readonly struct TokenHeatColor
{
	#region Fields

	/// <summary>
	/// Transparent / no heat.
	/// </summary>
	public static readonly TokenHeatColor None = new(0, 0, 0, 0);

	#endregion

	#region Constructors

	/// <summary>
	/// Creates heat channels.
	/// </summary>
	public TokenHeatColor(byte a, byte r, byte g, byte b)
	{
		A = a;
		R = r;
		G = g;
		B = b;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Alpha 0–255.
	/// </summary>
	public byte A { get; }

	/// <summary>
	/// Blue 0–255.
	/// </summary>
	public byte B { get; }

	/// <summary>
	/// Green 0–255.
	/// </summary>
	public byte G { get; }

	/// <summary>
	/// True when the tint is fully transparent.
	/// </summary>
	public bool IsNone => A == 0;

	/// <summary>
	/// Red 0–255.
	/// </summary>
	public byte R { get; }

	#endregion
}