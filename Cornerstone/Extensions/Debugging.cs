#region References

using System.Diagnostics;

#endregion

namespace Cornerstone.Extensions;

public static class Debugging
{
	#region Methods

	public static void BreakIfAttached()
	{
		#if DEBUG
		if (Debugger.IsAttached)
		{
			Debugger.Break();
		}
		#endif
	}

	#endregion
}