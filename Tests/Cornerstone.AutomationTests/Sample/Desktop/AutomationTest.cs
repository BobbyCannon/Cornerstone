#region References

using System;
using System.IO;
using System.Linq;
using Cornerstone.Automation;
using Cornerstone.UnitTests;

#endregion

namespace Cornerstone.AutomationTests.Sample.Desktop;

public class AutomationTest : CornerstoneUnitTest
{
	#region Constructors

	static AutomationTest()
	{
		// Prefer a freshly built sample under this repo; fall back to common local publish layouts.
		var repoSample = Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory,
			"..", "..", "..", "..", "..",
			"Cornerstone.Sample.Desktop", "bin", "Debug", "net10.0-windows10.0.26100.0",
			"Cornerstone.Sample.Desktop.exe"));

		FilePaths =
		[
			repoSample,
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
				"Cornerstone.Sample.Desktop", "Cornerstone.Sample.Desktop.exe")
		];
	}

	#endregion

	#region Properties

	public static string[] FilePaths { get; }

	#endregion

	#region Methods

	protected Application StartTestApplication()
	{
		var filePath = FilePaths.FirstOrDefault(File.Exists);
		var app = Application.Create(filePath);
		app.AutoClose = true;
		return app;
	}

	#endregion
}