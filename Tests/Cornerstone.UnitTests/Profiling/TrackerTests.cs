#region References

using System;
using System.Linq;
using Cornerstone.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class TrackerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void InitialSessionEventShouldBeAddedOnStart()
	{
		var repository = new MemoryTrackerRepository(null);
		var tracker = Tracker.Start(repository, this, null);
		tracker.Dispose();

		Assert.AreEqual(1, repository.Paths.Count);
		Assert.AreEqual("Session", repository.Paths[0].Name);

		// todo: maybe add this support back? maybe?

		//var expectedAssembly = typeof(Tracker).Assembly.GetName();
		//var expectedVersion = expectedAssembly.Version;
		//var values = repository.Paths[0].Values.ToList();

		//Assert.AreEqual(4, values.Count);
		//Assert.AreEqual(".NET Version", values[0].Name);
		//Assert.AreEqual(Environment.Version.ToString(), values[0].Value);
		//Assert.AreEqual("Application Bitness", values[1].Name);
		//Assert.AreEqual(Environment.Is64BitProcess ? "64" : "32", values[1].Value);
		//Assert.AreEqual("Application Name", values[2].Name);
		//Assert.AreEqual(expectedAssembly.Name, values[2].Value);
		//Assert.AreEqual("Application Version", values[3].Name);
		//Assert.AreEqual(expectedVersion.ToString(), values[3].Value);
	}

	#endregion
}