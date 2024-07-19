#region References

using System;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class UpdateableTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ReflectionCloneable()
	{
		var details = new ProcessDetails
		{
			Arguments = "--Version",
			FilePath = "git.exe",
			ProcessId = 12345,
			StartedOn = new DateTime(2024, 05, 15, 08, 45, 00, 00, DateTimeKind.Utc),
			StoppedOn = new DateTime(2024, 05, 15, 08, 45, 00, 123, DateTimeKind.Utc),
			WorkingDirectory = "C:\\Workspaces",
			ExitCode = 0,
			WasCancelled = false
		};

		AreEqual(TimeSpan.FromMilliseconds(123), details.Duration);

		var actual = details.ShallowClone();

		AreEqual(details, actual);
	}

	#endregion
}