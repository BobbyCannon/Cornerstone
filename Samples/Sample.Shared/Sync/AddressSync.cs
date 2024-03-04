#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.Sync;

#endregion

namespace Sample.Shared.Sync;

/// <inheritdoc cref="IAddressSync" />
public class AddressSync : SyncModel, IAddressSync
{
	#region Properties

	/// <inheritdoc />
	public string City { get; set; }

	/// <inheritdoc />
	public string Line1 { get; set; }

	/// <inheritdoc />
	public string Line2 { get; set; }

	/// <inheritdoc />
	public string Postal { get; set; }

	/// <inheritdoc />
	public string State { get; set; }

	#endregion
}

/// <summary>
/// Represents the address sync model.
/// </summary>
public interface IAddressSync
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