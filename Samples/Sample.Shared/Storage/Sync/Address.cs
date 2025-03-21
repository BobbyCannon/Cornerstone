#region References

using Cornerstone.Sync;

#endregion

namespace Sample.Shared.Storage.Sync;

/// <summary>
/// Represents the public address model.
/// </summary>
public class Address : SyncModel, IAddress
{
	#region Properties

	/// <summary>
	/// The city for the address.
	/// </summary>
	public string City { get; set; }

	/// <summary>
	/// The line 1 for the address.
	/// </summary>
	public string Line1 { get; set; }

	/// <summary>
	/// The line 2 for the address.
	/// </summary>
	public string Line2 { get; set; }

	/// <summary>
	/// The postal for the address.
	/// </summary>
	public string Postal { get; set; }

	/// <summary>
	/// The state for the address.
	/// </summary>
	public string State { get; set; }

	#endregion
}

/// <summary>
/// Represents the address sync model.
/// </summary>
public interface IAddress
{
	#region Properties

	/// <summary>
	/// The city for the address.
	/// </summary>
	public string City { get; set; }

	/// <summary>
	/// The line 1 for the address.
	/// </summary>
	public string Line1 { get; set; }

	/// <summary>
	/// The line 2 for the address.
	/// </summary>
	public string Line2 { get; set; }

	/// <summary>
	/// The postal for the address.
	/// </summary>
	public string Postal { get; set; }

	/// <summary>
	/// The state for the address.
	/// </summary>
	public string State { get; set; }

	#endregion
}