#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Web;

/// <summary>
/// Represents a service request containing a collection.
/// </summary>
/// <typeparam name="T"> The type of the item collection. </typeparam>
public partial class ServiceRequest<T> : ServiceRequest
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of a service request.
	/// </summary>
	public ServiceRequest() : this([])
	{
	}

	/// <summary>
	/// Initializes an instance of a service request.
	/// </summary>
	public ServiceRequest(params T[] collection) : this(collection.ToList())
	{
	}

	/// <summary>
	/// Initializes an instance of a service request.
	/// </summary>
	public ServiceRequest(IEnumerable<T> collection)
	{
		Collection = collection.ToList();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The collection of items to include in the request.
	/// </summary>
	public IList<T> Collection { get; set; }

	#endregion
}

/// <summary>
/// Represents a service request.
/// </summary>
[SourceReflection]
public partial class ServiceRequest : CornerstoneObject
{
	#region Properties

	/// <summary>
	/// The optional collection of filter values.
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public IDictionary<string, string> Filters { get; set; }

	/// <summary>
	/// The values to be included in the results. Defaults to an empty collection.
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public IList<string> Including { get; set; }

	/// <summary>
	/// The optional collection of request options.
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public IDictionary<string, string> Options { get; set; }

	/// <summary>
	/// The number of items to skip.
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public int Skip { get; set; }

	/// <summary>
	/// The number of items requested.
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public int Take { get; set; }

	#endregion
}