#region References

using System.Linq;
using Cornerstone.VisualStudio.Core.Preview;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class PreviewHostSelectorTests
{
	#region Methods

	[TestMethod]
	public void FrameworkPresentationPrefersSampleDesktopOverOtherDesktopHosts()
	{
		var sample = Rank("Cornerstone.Presentation", "Cornerstone.Sample.Desktop");
		var farm = Rank("Cornerstone.Presentation", "Album.Desktop");
		Assert.IsTrue(sample < farm);
		Assert.AreEqual(1, sample);
		Assert.AreEqual(3, farm);
	}

	[TestMethod]
	public void ThemeAndDiagnosticsPreferSampleDesktop()
	{
		Assert.AreEqual(1, Rank("Cornerstone.Presentation", "Cornerstone.Sample.Desktop"));
		Assert.AreEqual(1, Rank("Cornerstone.Presentation.Diagnostics", "Cornerstone.Sample.Desktop"));
		Assert.AreEqual(1, Rank("Cornerstone.Presentation.Sandbox", "Cornerstone.Sample.Desktop"));
	}

	[TestMethod]
	public void MissingSampleDesktopFallsBackToOtherDesktopHost()
	{
		var farm = Rank("Cornerstone.Presentation", "Album.Desktop");
		var other = Rank("Cornerstone.Presentation", "Other.Desktop");
		Assert.AreEqual(3, farm);
		Assert.AreEqual(3, other);
	}

	[TestMethod]
	public void SampleLibraryUsesConventionNotFrameworkPreferredRank()
	{
		var sample = Rank("Cornerstone.Sample", "Cornerstone.Sample.Desktop");
		Assert.AreEqual(2, sample);
		Assert.IsFalse(PreviewHostSelector.IsFrameworkPresentationLibrary("Cornerstone.Sample"));
	}

	[TestMethod]
	public void ProductLibraryPrefersOwnDesktopEvenWhenSampleDesktopPresent()
	{
		var own = Rank("Album", "Album.Desktop");
		var sample = Rank("Album", "Cornerstone.Sample.Desktop");
		Assert.AreEqual(2, own);
		Assert.AreEqual(3, sample);
		Assert.IsTrue(own < sample);
	}

	[TestMethod]
	public void ProductControlsLibraryPrefersSiblingDesktopOverSampleDesktop()
	{
		var own = Rank("Album.Controls", "Album.Desktop");
		var sample = Rank("Album.Controls", "Cornerstone.Sample.Desktop");
		Assert.IsTrue(own < sample);
		Assert.IsTrue(PreviewHostSelector.IsRelatedDesktopHostName("Album.Desktop", "Album.Controls"));
	}

	[TestMethod]
	public void SelfHostExecutableIsBestRank()
	{
		var self = Rank("Cornerstone.Sample.Desktop", "Cornerstone.Sample.Desktop", candidateIsXamlProject: true);
		var other = Rank("Cornerstone.Sample.Desktop", "Album.Desktop");
		Assert.AreEqual(0, self);
		Assert.IsTrue(self < other);
	}

	[TestMethod]
	public void OrderedHostsForPresentationPutSampleDesktopFirst()
	{
		var names = new[]
		{
			"Album.Desktop",
			"Other.Desktop",
			"Cornerstone.Sample.Desktop",
			"Cornerstone.VisualStudio.Documentation"
		};

		var ordered = names
			.Select(n => new { Name = n, Rank = Rank("Cornerstone.Presentation", n, hasDesktopStack: true) })
			.OrderBy(x => x.Rank)
			.ThenBy(x => x.Name)
			.Select(x => x.Name)
			.ToArray();

		Assert.AreEqual("Cornerstone.Sample.Desktop", ordered[0]);
	}

	private static int Rank(
		string xamlProjectName,
		string candidateName,
		bool candidateIsXamlProject = false,
		bool hasDesktopStack = true,
		bool isStartupProject = false,
		bool directlyReferencesXamlProject = true)
	{
		return PreviewHostSelector.RankHost(new PreviewHostRankContext(
			xamlProjectName,
			candidateName,
			candidateIsXamlProject,
			hasDesktopStack,
			isStartupProject,
			directlyReferencesXamlProject));
	}

	#endregion
}
