#region References

using System;
using System.IO;
using Cornerstone.GrokMonitor.GrokUsage;
using Cornerstone.GrokMonitor.GrokUsage.Services;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.GrokMonitor.GrokUsage;

[TestClass]
public class GrokPathsArchiveTests : GrokMonitorUnitTest
{
	#region Methods

	[TestMethod]
	public void UsageArchiveUsesHomeFolderName()
	{
		using var appData = new IsolatedAppData();
		var personal = Path.Combine(appData.Homes, ".grok");
		var work = Path.Combine(appData.Homes, ".grok-work");
		Directory.CreateDirectory(personal);
		Directory.CreateDirectory(work);

		var personalArchive = GrokPaths.GetUsageArchiveDirectory(personal, appData.Runtime);
		var workArchive = GrokPaths.GetUsageArchiveDirectory(work, appData.Runtime);

		AreEqual(".grok", Path.GetFileName(personalArchive));
		AreEqual(".grok-work", Path.GetFileName(workArchive));
		AreEqual(Path.Combine(appData.Path, GrokPaths.UsageArchiveRootName, ".grok"), personalArchive);
	}

	[TestMethod]
	public void UsageArchiveSuffixesWhenFolderNameAlreadyClaimed()
	{
		using var appData = new IsolatedAppData();
		var first = Path.Combine(appData.Homes, "a", ".grok");
		var second = Path.Combine(appData.Homes, "b", ".grok");
		Directory.CreateDirectory(first);
		Directory.CreateDirectory(second);

		var firstArchive = GrokPaths.GetUsageArchiveDirectory(first, appData.Runtime);
		GrokPaths.WriteUsageArchiveHomeFile(firstArchive, first);

		var secondArchive = GrokPaths.GetUsageArchiveDirectory(second, appData.Runtime);
		IsTrue(Path.GetFileName(secondArchive).StartsWith(".grok-", StringComparison.OrdinalIgnoreCase));
		AreNotEqual(firstArchive, secondArchive);

		var firstAgain = GrokPaths.GetUsageArchiveDirectory(first, appData.Runtime);
		AreEqual(firstArchive, firstAgain);
	}

	#endregion

	#region Classes

	private sealed class IsolatedAppData : IDisposable
	{
		public IsolatedAppData()
		{
			Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GrokArchivePaths_" + Guid.NewGuid().ToString("N"));
			Homes = System.IO.Path.Combine(Path, "homes");
			Directory.CreateDirectory(Homes);
			Runtime = new RuntimeInformation();
			Runtime.SetOverride(nameof(IRuntimeInformation.ApplicationDataLocation), Path);
		}

		public string Homes { get; }

		public string Path { get; }

		public RuntimeInformation Runtime { get; }

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
				// best-effort
			}
		}
	}

	#endregion
}
