#region References

using System;
using Cornerstone.Exceptions;

#endregion

namespace Cornerstone.Location;

/// <summary>
/// Location exception
/// </summary>
public class LocationProviderException : CornerstoneException
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of the location provider exception.
	/// </summary>
	public LocationProviderException() : this(LocationProviderError.Unknown)
	{
	}

	/// <summary>
	/// Initializes an instance of the location provider exception.
	/// </summary>
	/// <param name="error"> The error for the location provider. </param>
	/// <param name="innerException"> An inner exception. </param>
	public LocationProviderException(LocationProviderError error, Exception innerException = null)
		: base($"A location provider error occurred: {error}", innerException)
	{
		if (!Enum.IsDefined(typeof(LocationProviderError), error))
		{
			throw new ArgumentException("The provider error is not a valid location provider value.", nameof(error));
		}

		Error = error;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The error for the location provider.
	/// </summary>
	public LocationProviderError Error { get; set; }

	#endregion
}