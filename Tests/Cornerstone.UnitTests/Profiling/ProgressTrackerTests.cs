#region References

using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class ProgressTrackerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Update()
	{
		var progress = new ProgressTracker();
		progress.Update(50, 0, 100);
		AreEqual(50, progress.Percent);
		AreEqual(50, progress.Value);
		
		progress.Update(51);
		AreEqual(51, progress.Percent);
		AreEqual(51, progress.Value);

		progress.UpdatePercent(-1);
		AreEqual(-1, progress.Percent);
		AreEqual(0, progress.Value);
	}

	#endregion
}