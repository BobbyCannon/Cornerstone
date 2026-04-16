#region References

using System.IO;
using Cornerstone.Automation;
using Cornerstone.UnitTests;

#endregion

namespace Cornerstone.AutomationTests.Sample.Desktop;

public class AutomationTest : CornerstoneUnitTest
{
	#region Constructors

	static AutomationTest()
	{
		FilePath = Path.Join(@"C:\Users\Bobby\Desktop\Cornerstone.Sample.Desktop\Cornerstone.Sample.Desktop.exe");
	}

	#endregion

	#region Properties

	public static string FilePath { get; }

	#endregion

	#region Methods

	protected Application StartTestApplication()
	{
		var app = Application.Create(FilePath);
		app.AutoClose = true;
		return app;
	}

	#endregion
}