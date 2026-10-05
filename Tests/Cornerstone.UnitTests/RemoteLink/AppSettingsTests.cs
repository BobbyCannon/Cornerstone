#region References

using System;
using System.IO;
using Cornerstone.Presentation.Theme.Theming;
using Cornerstone.RemoteLink.Keystone.State;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.RemoteLink;

[TestClass]
public class AppSettingsTests : RemoteLinkUnitTest
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
	public void FinalizeLoadRestoresVncDefaults()
	{
		using var temp = new TemporaryDirectory();
		var runtime = GetInstance<RuntimeInformation>();
		runtime.SetOverride(nameof(IRuntimeInformation.ApplicationDataLocation), temp.Path);

		var settings = new AppSettings(runtime);
		settings.LoadLifecycle();
		settings.VncHost = " ";
		settings.VncPort = 70000;
		settings.Save(true);

		var loaded = new AppSettings(runtime);
		loaded.LoadLifecycle();

		AreEqual("127.0.0.1", loaded.VncHost);
		AreEqual(5900, loaded.VncPort);
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
		settings.VncHost = "192.168.1.50";
		settings.VncPort = 5901;
		settings.WindowLocation.Left = 120;
		settings.WindowLocation.Top = 80;
		settings.WindowLocation.Width = 1400;
		settings.WindowLocation.Height = 900;
		settings.WindowLocation.Maximized = true;
		settings.Save(true);

		var filePath = Path.Combine(temp.Path, "ApplicationSettings.json");
		IsTrue(File.Exists(filePath));

		var loaded = new AppSettings(runtime);
		loaded.LoadLifecycle();

		AreEqual(ThemeColor.Green, loaded.ThemeColor);
		AreEqual(ThemeMode.Light, loaded.ThemeMode);
		AreEqual(ThemeDensity.Compact, loaded.ThemeDensity);
		AreEqual("192.168.1.50", loaded.VncHost);
		AreEqual(5901, loaded.VncPort);
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

	#region Classes

	private sealed class TemporaryDirectory : IDisposable
	{
		#region Constructors

		public TemporaryDirectory()
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RemoteLink.AppSettings." + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Path);
		}

		#endregion

		#region Properties

		public string Path { get; }

		#endregion

		#region Methods

		public void Dispose()
		{
			try
			{
				if (Directory.Exists(Path))
				{
					Directory.Delete(Path, true);
				}
			}
			catch
			{
				// Best-effort cleanup for temp test data.
			}
		}

		#endregion
	}

	#endregion
}