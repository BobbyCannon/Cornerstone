#region References

using System;
using System.Collections.Generic;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class UriExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void UriToApplicationArguments()
	{
		var scenarios = new Dictionary<Uri, string[]>
		{
			{
				new Uri("https://localhost/Account?Name=\"John\"", UriKind.Absolute),
				["-Schema", "https", "-Host", "localhost", "-Path", "/Account", "-Query", "?Name=%22John%22"]
			},
			{
				new Uri("https://localhost/Page?id=12", UriKind.Absolute),
				["-Schema", "https", "-Host", "localhost", "-Path", "/Page", "-Query", "?id=12"]
			},
			{
				new Uri("https://localhost?id=12", UriKind.Absolute),
				["-Schema", "https", "-Host", "localhost", "-Path", "/", "-Query", "?id=12"]
			},
			{
				new Uri("https://localhost/Page/64", UriKind.Absolute),
				["-Schema", "https", "-Host", "localhost", "-Path", "/Page/64"]
			},
			{
				new Uri("https://localhost:7169", UriKind.Absolute),
				["-Schema", "https", "-Host", "localhost", "-Path", "/"]
			}
		};

		foreach (var scenario in scenarios)
		{
			scenario.Key.Dump();
			var actual = scenario.Key.ToApplicationArguments();
			AreEqual(scenario.Value, actual, () => actual.DumpCSharp());
		}
	}

	#endregion
}