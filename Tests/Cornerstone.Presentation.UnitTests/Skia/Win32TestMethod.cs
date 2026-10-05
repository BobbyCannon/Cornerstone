#region References

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cornerstone.Presentation.UnitTests.Helpers;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia;

internal class Win32TestMethodAttribute : PresentationTestMethodAttribute
{
	#region Constructors

	public Win32TestMethodAttribute(
		string message,
		[CallerFilePath] string sourceFilePath = null,
		[CallerLineNumber] int sourceLineNumber = -1)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Skip = message;
		}
	}

	#endregion
}