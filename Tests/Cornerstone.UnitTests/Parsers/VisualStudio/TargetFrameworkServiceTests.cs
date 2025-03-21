#region References

using System;
using Cornerstone.Parsers.VisualStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.VisualStudio;

[TestClass]
public class TargetFrameworkServiceTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetAncestors()
	{
		var actual = TargetFrameworkService.GetAncestors(TargetFrameworkType.NetStandard21);
		var expected = Array.Empty<TargetFramework>();

		AreEqual(expected, actual);

		actual = TargetFrameworkService.GetAncestors(TargetFrameworkType.NetStandard16);
		expected =
		[
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard21),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard20)
		];

		AreEqual(expected, actual);

		actual = TargetFrameworkService.GetAncestors(TargetFrameworkType.NetStandard10);
		expected =
		[
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard21),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard20),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard16),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard15),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard14),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard13),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard12),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard11)
		];

		AreEqual(expected, actual);
	}

	[TestMethod]
	public void GetCompatibleFrameworkShouldFindMatch()
	{
		var scenarios = new (string current, string suggested)[]
		{
			("net9.0-windows10.0.19041.0", "net9.0-windows10.0.19041.0"),
			("net9.0-windows10.0.19041.0", "net9.0-windows"),
			("net9.0-windows", "net9.0"),
			("net9.0", "net9.0"),
			("net9.0", "netstandard2.1"),
			("net9.0", "netstandard2.0"),
			("net9.0", "netstandard1.4"),
			("net5.0", "netstandard1.1")
		};

		foreach (var scenario in scenarios)
		{
			var current = new[] { TargetFrameworkService.GetOrAddFramework(scenario.current) };
			var suggested = new[] { TargetFrameworkService.GetOrAddFramework(scenario.suggested) };
			var actual = TargetFrameworkService.GetCompatibleFramework(current, suggested);
			IsNotNull(actual);
			AreEqual(scenario.suggested, actual.Moniker);
		}
	}

	[TestMethod]
	public void GetDescendants()
	{
		var actual = TargetFrameworkService.GetDescendants(TargetFrameworkType.NetStandard21);
		TargetFramework[] expected =
		[
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard20),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard16),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard15),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard14),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard13),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard12),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard11),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard10)
		];

		AreEqual(expected, actual);

		actual = TargetFrameworkService.GetDescendants(TargetFrameworkType.NetStandard16);
		expected =
		[
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard15),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard14),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard13),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard12),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard11),
			TargetFrameworkService.GetByType(TargetFrameworkType.NetStandard10)
		];

		AreEqual(expected, actual);
	}

	[TestMethod]
	public void GetOrAddFramework()
	{
		var actual = TargetFrameworkService.GetOrAddFramework("net9.0-windows10.0.19041.0");
		AreEqual("net9.0-windows10.0.19041.0", actual.Moniker);
		AreEqual("net9.0", actual.PlatformMoniker);
		AreEqual("windows", actual.OperatingSystem);
		AreEqual("10.0.19041.0", actual.OperatingSystemVersion);
		AreEqual(TargetFrameworkType.Net9Windows, actual.Type);
		IsNull(actual.Parent);
		IsNotNull(actual.Platform);
		IsNotNull(actual.PlatformMoniker, actual.Platform.Moniker);
	}

	[TestMethod]
	public void OrderFrameworks()
	{
		var expected = new[]
		{
			TargetFrameworkService.GetOrAddFramework("net9.0"),
			TargetFrameworkService.GetOrAddFramework("net7.0"),
			TargetFrameworkService.GetOrAddFramework("net6.0-windows"),
			TargetFrameworkService.GetByType(TargetFrameworkType.Net11)
		};

		var unordered = new[]
		{
			TargetFrameworkService.GetByType(TargetFrameworkType.Net11),
			TargetFrameworkService.GetOrAddFramework("net9.0"),
			TargetFrameworkService.GetOrAddFramework("net6.0-windows"),
			TargetFrameworkService.GetOrAddFramework("net7.0")
		};

		var actual = TargetFrameworkService.OrderFrameworks(unordered);
		IsNotNull(actual);

		AreEqual(expected, actual);
	}

	#endregion
}