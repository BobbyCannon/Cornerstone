#region References

using System;
using System.IO;
using Cornerstone.Avalonia.Themes;
using Cornerstone.GrokMonitor.Keystone.State;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.GrokMonitor;

[TestClass]
public class AppSettingsTests : GrokMonitorUnitTest
{
	#region Methods

	[TestMethod]
	public void FinalizeLoadCreatesDefaultWindowLocation()
	{
		using var temp = new TemporaryDirectory();
		var runtime = GetInstance<RuntimeInformation>();
		runtime.SetOverride(nameof(IRuntimeInformation.ApplicationDataLocation), temp.Path);

		var settings = new AppSettings(runtime);
		settings.LoadLifecycle();

		IsNotNull(settings.WindowLocation);
		IsTrue(settings.WindowLocation.IsDefaultLocation());
	}

	[TestMethod]
	public void SaveAndLoadJsonRoundTrip()
	{
		using var temp = new TemporaryDirectory();
		var runtime = GetInstance<RuntimeInformation>();
		runtime.SetOverride(nameof(IRuntimeInformation.ApplicationDataLocation), temp.Path);

		var settings = new AppSettings(runtime);
		settings.LoadLifecycle();
		settings.ThemeColor = ThemeColor.Green;
		settings.ThemeMode = ThemeMode.Light;
		settings.ThemeDensity = ThemeDensity.Compact;
		settings.SessionTokenHeatEnabled = false;
		// Values above default soft and below default hot so load-order sanitize does not rewrite.
		settings.SessionTokenHeatSoftTokens = 2_000_000;
		settings.SessionTokenHeatHotTokens = 8_000_000;
		settings.WindowLocation.Left = 120;
		settings.WindowLocation.Top = 80;
		settings.WindowLocation.Width = 1400;
		settings.WindowLocation.Height = 900;
		settings.WindowLocation.Maximized = true;
		settings.Save(force: true);

		var filePath = Path.Combine(temp.Path, "ApplicationSettings.json");
		IsTrue(File.Exists(filePath));

		var loaded = new AppSettings(runtime);
		loaded.LoadLifecycle();

		AreEqual(ThemeColor.Green, loaded.ThemeColor);
		AreEqual(ThemeMode.Light, loaded.ThemeMode);
		AreEqual(ThemeDensity.Compact, loaded.ThemeDensity);
		AreEqual(false, loaded.SessionTokenHeatEnabled);
		AreEqual(2_000_000L, loaded.SessionTokenHeatSoftTokens);
		AreEqual(8_000_000L, loaded.SessionTokenHeatHotTokens);
		IsNotNull(loaded.WindowLocation);
		AreEqual(120, loaded.WindowLocation.Left);
		AreEqual(80, loaded.WindowLocation.Top);
		AreEqual(1400, loaded.WindowLocation.Width);
		AreEqual(900, loaded.WindowLocation.Height);
		AreEqual(true, loaded.WindowLocation.Maximized);
	}

	[TestMethod]
	public void SavePersistsNestedWindowLocationWithoutForce()
	{
		// Nested WindowLocation changes do not set SettingsFile's own PropertyChanged
		// dirty flag; Save must still persist via HasChanges() (SettingsManager pattern).
		using var temp = new TemporaryDirectory();
		var runtime = GetInstance<RuntimeInformation>();
		runtime.SetOverride(nameof(IRuntimeInformation.ApplicationDataLocation), temp.Path);

		var settings = new AppSettings(runtime);
		settings.LoadLifecycle();
		settings.ResetHasChanges();

		settings.WindowLocation.Left = 42;
		settings.WindowLocation.Top = 64;
		settings.WindowLocation.Width = 1280;
		settings.WindowLocation.Height = 720;
		settings.WindowLocation.Maximized = false;
		IsTrue(settings.HasChanges());

		settings.Save();

		var filePath = Path.Combine(temp.Path, "ApplicationSettings.json");
		IsTrue(File.Exists(filePath));
		IsFalse(settings.HasChanges());

		var loaded = new AppSettings(runtime);
		loaded.LoadLifecycle();
		AreEqual(42, loaded.WindowLocation.Left);
		AreEqual(64, loaded.WindowLocation.Top);
		AreEqual(1280, loaded.WindowLocation.Width);
		AreEqual(720, loaded.WindowLocation.Height);
		AreEqual(false, loaded.WindowLocation.Maximized);
	}

	[TestMethod]
	public void WindowLocationHasChangesPropagates()
	{
		using var temp = new TemporaryDirectory();
		var runtime = GetInstance<RuntimeInformation>();
		runtime.SetOverride(nameof(IRuntimeInformation.ApplicationDataLocation), temp.Path);

		var settings = new AppSettings(runtime);
		settings.LoadLifecycle();
		settings.ResetHasChanges();
		IsFalse(settings.HasChanges());

		settings.WindowLocation.Width = 1600;
		IsTrue(settings.HasChanges());

		settings.ResetHasChanges();
		IsFalse(settings.HasChanges());
		IsFalse(settings.WindowLocation.HasChanges());
	}

	#endregion

	#region Nested Types

	private sealed class TemporaryDirectory : IDisposable
	{
		public TemporaryDirectory()
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GrokMonitor.AppSettings." + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Path);
		}

		public string Path { get; }

		public void Dispose()
		{
			try
			{
				if (Directory.Exists(Path))
				{
					Directory.Delete(Path, recursive: true);
				}
			}
			catch
			{
				// Best-effort cleanup for temp test data.
			}
		}
	}

	#endregion
}
