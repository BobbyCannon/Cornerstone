#region References

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;

#endregion

namespace Cornerstone.Web.AspNetCore;

/// <summary>
/// Provides programmatic configuration for JSON formatters using Cornerstone.JSON.
/// </summary>
public class JsonOptions : IEnumerable<ICompatibilitySwitch>
{
	#region Fields

	private readonly IReadOnlyList<ICompatibilitySwitch> _switches = Array.Empty<ICompatibilitySwitch>();

	#endregion

	#region Methods

	IEnumerator<ICompatibilitySwitch> IEnumerable<ICompatibilitySwitch>.GetEnumerator()
	{
		return _switches.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _switches.GetEnumerator();
	}

	#endregion
}