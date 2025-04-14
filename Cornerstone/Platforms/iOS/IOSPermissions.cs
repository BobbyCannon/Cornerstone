#region References

using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Platforms.iOS;

public class IOSPermissions : Permissions
{
	#region Constructors

	public IOSPermissions(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion
}