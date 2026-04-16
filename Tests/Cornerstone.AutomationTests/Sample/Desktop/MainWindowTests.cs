#region References

using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.AutomationTests.Sample.Desktop;

[TestClass]
public class MainWindowTests : AutomationTest
{
	#region Methods

	[TestMethod]
	public void MoveWindowWithLocationAndSize()
	{
		using var app = StartTestApplication();
		app.MoveWindow(101, 102, 1024, 768);

		AreEqual(101, app.Location.X);
		AreEqual(102, app.Location.Y);
		AreEqual(1024, app.Size.Width);
		AreEqual(768, app.Size.Height);

		app.MoveWindow(200, 199, 800, 600);

		AreEqual(200, app.Location.X);
		AreEqual(199, app.Location.Y);
		AreEqual(800, app.Size.Width);
		AreEqual(600, app.Size.Height);
	}

	#endregion
}