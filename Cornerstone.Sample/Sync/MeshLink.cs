namespace Cornerstone.Sample.Sync;

/// <summary>
/// One directed sync. The first node is the client. The second is the server slot.
/// Spokes use a hub server. Rim links use a peer in the server slot.
/// </summary>
public enum MeshLink
{
	NorthToCenter = 0,
	EastToCenter = 1,
	SouthToCenter = 2,
	WestToCenter = 3,
	NorthToEast = 4,
	EastToSouth = 5,
	SouthToWest = 6,
	WestToNorth = 7
}
