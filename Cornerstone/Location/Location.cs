namespace Cornerstone.Location;

public class Location : ILocation<HorizontalLocation, VerticalLocation>
{
	#region Constructors

	public Location()
	{
		Horizontal = new HorizontalLocation();
		Vertical = new VerticalLocation();
	}

	#endregion

	#region Properties

	public HorizontalLocation Horizontal { get; set; }
	public VerticalLocation Vertical { get; set; }

	#endregion
}

/// <summary>
/// Represents a provider location.
/// </summary>
public interface ILocation<THorizontalLocation, TVerticalLocation>
	where THorizontalLocation : class, IHorizontalLocation
	where TVerticalLocation : class, IVerticalLocation
{
	#region Properties

	/// <summary>
	/// The horizontal location.
	/// </summary>
	THorizontalLocation Horizontal { get; set; }

	/// <summary>
	/// The vertical location.
	/// </summary>
	TVerticalLocation Vertical { get; set; }

	#endregion
}