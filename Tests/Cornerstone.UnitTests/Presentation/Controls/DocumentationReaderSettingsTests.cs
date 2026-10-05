#region References

using System;
using System.IO;
using Cornerstone.Presentation.Documentation;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Controls;

[TestClass]
public class DocumentationReaderSettingsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetDirectoryUsesRuntimeInformationPerApplication()
	{
		using var temp = new TemporaryDirectory();
		var information = new RuntimeInformation();
		information.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationDataLocation), temp.Path);
		information.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationName), "Guides.Documentation");

		var cornerstone = new RuntimeInformation();
		cornerstone.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationDataLocation), temp.Path);
		cornerstone.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationName), "Cornerstone.Documentation");

		var epicDirectory = DocumentationReaderSettings.GetDirectory(information);
		var cornerstoneDirectory = DocumentationReaderSettings.GetDirectory(cornerstone);

		AreEqual(Path.Combine(temp.Path, "Guides.Documentation"), epicDirectory);
		AreEqual(Path.Combine(temp.Path, "Cornerstone.Documentation"), cornerstoneDirectory);
		AreNotEqual(epicDirectory, cornerstoneDirectory);

		var alreadyNamed = Path.Combine(temp.Path, "Cornerstone.Documentation");
		cornerstone.SetPlatformOverride(nameof(IRuntimeInformation.ApplicationDataLocation), alreadyNamed);
		AreEqual(alreadyNamed, DocumentationReaderSettings.GetDirectory(cornerstone));
	}

	[TestMethod]
	public void SaveAndLoadReadingWidthAndWindowLocation()
	{
		using var temp = new TemporaryDirectory();
		var settings = new DocumentationReaderSettings(temp.Path);
		settings.LoadLifecycle();

		IsNotNull(settings.WindowLocation);
		IsTrue(settings.WindowLocation.IsDefaultLocation());
		AreEqual(DocumentationReadingWidth.Column, settings.ReadingWidth);

		settings.ReadingWidth = DocumentationReadingWidth.Full;
		settings.WindowLocation.Left = 40;
		settings.WindowLocation.Top = 60;
		settings.WindowLocation.Width = 1440;
		settings.WindowLocation.Height = 900;
		settings.WindowLocation.Maximized = true;
		settings.Save();

		var loaded = new DocumentationReaderSettings(temp.Path);
		loaded.LoadLifecycle();

		AreEqual(DocumentationReadingWidth.Full, loaded.ReadingWidth);
		AreEqual(40, loaded.WindowLocation.Left);
		AreEqual(60, loaded.WindowLocation.Top);
		AreEqual(1440, loaded.WindowLocation.Width);
		AreEqual(900, loaded.WindowLocation.Height);
		IsTrue(loaded.WindowLocation.Maximized);
		IsTrue(File.ReadAllText(Path.Combine(temp.Path, "ApplicationSettings.json")).Contains("Full"));
	}

	#endregion

	#region Classes

	private sealed class TemporaryDirectory : IDisposable
	{
		#region Constructors

		public TemporaryDirectory()
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DocumentationReader.Settings." + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Path);
		}

		#endregion

		#region Properties

		public string Path { get; }

		#endregion

		#region Methods

		public void Dispose()
		{
			if (Directory.Exists(Path))
			{
				Directory.Delete(Path, true);
			}
		}

		#endregion
	}

	#endregion
}