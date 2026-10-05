#region References

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[Flags]
internal enum TestPlatforms
{
	Windows = 0x01,
	MacOS = 0x02,
	Linux = 0x04,
	All = Windows | MacOS | Linux
}

internal class PlatformTestMethodAttribute : PresentationTestMethodAttribute
{
	#region Constructors

	public PlatformTestMethodAttribute(
		TestPlatforms platforms,
		string reason = null,
		[CallerFilePath] string sourceFilePath = null,
		[CallerLineNumber] int sourceLineNumber = -1)
	{
		Platforms = platforms;
		if (!IsSupported())
		{
			Skip = "Ignored on " + RuntimeInformation.OSDescription +
				(reason is not null ? " reason: '" + reason + "'" : "");
		}
	}

	#endregion

	#region Properties

	public TestPlatforms Platforms { get; }

	#endregion

	#region Methods

	private bool IsSupported()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return Platforms.HasFlag(TestPlatforms.Windows);
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return Platforms.HasFlag(TestPlatforms.MacOS);
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return Platforms.HasFlag(TestPlatforms.Linux);
		}
		return false;
	}

	#endregion
}