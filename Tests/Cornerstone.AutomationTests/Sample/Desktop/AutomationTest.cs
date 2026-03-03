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
		FilePath = Path.Join(
			@"C:\Workspaces\EpicSolution\Cornerstone",
			@"Cornerstone.Sample.Desktop\bin\Release",
			@"net10.0-windows10.0.26100.0\win-x64\publish",
			"Cornerstone.Sample.Desktop.exe"
		);
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