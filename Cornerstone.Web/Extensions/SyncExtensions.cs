#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Sync;
using Microsoft.Extensions.Primitives;

#endregion

namespace Cornerstone.Web.Extensions;

public static class SyncClientDetailsExtensions
{
	#region Methods

	/// <summary>
	/// Load the sync client details into the provided sync options.
	/// </summary>
	/// <param name="device"> The device to load options into. </param>
	/// <param name="values"> The values to load. </param>
	public static void Load(this SyncClientDetails device, IDictionary<string, StringValues> values)
	{
		device.Load(values.ToDictionary(x => x.Key, x => string.Join("", x.Value.ToArray())));
	}

	#endregion
}