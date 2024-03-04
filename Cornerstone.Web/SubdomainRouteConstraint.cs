#region References

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

#endregion

namespace Cornerstone.Web;

/// <summary>
/// Subdomain routing
/// </summary>
public class SubdomainRouteConstraint : IRouteConstraint
{
	#region Fields

	private readonly string _subdomain;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the subdomain route constraint.
	/// </summary>
	/// <param name="subdomain"> The subdomain  </param>
	public SubdomainRouteConstraint(string subdomain)
	{
		_subdomain = subdomain;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
	{
		if (httpContext == null)
		{
			return true;
		}

		var url = httpContext.Request.Headers["HOST"].ToString();
		var index = url.IndexOf(".");

		if (index < 0)
		{
			return false;
		}

		var subDomain = url.Split('.')[0];
		return subDomain == _subdomain;
	}

	#endregion
}