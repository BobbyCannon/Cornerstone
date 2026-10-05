#region References

using System.Runtime.CompilerServices;
using Cornerstone.Presentation.UnitTests.Helpers;

#endregion

namespace Cornerstone.Presentation.UnitTests.Leak;

/// <summary>
/// Use on leak tests where objects are somehow kept rooted in debug mode.
/// </summary>
internal sealed class ReleaseTestMethodAttribute : PresentationTestMethodAttribute
{
	#region Constructors

	public ReleaseTestMethodAttribute(
		[CallerFilePath] string sourceFilePath = null,
		[CallerLineNumber] int sourceLineNumber = -1
	)
	{
		#if DEBUG
		Skip = "Only runs in Release mode";
		#endif
	}

	#endregion
}