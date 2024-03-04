#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Parsers.VisualStudio;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

#endregion

namespace Cornerstone.UnitTests.Parsers.VisualStudio;

[TestClass]
public class TargetFrameworkMonikerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Debugging()
	{
		var framework = TargetFrameworkService.GetOrAddFramework("net8.0-windows10.0.19041.0");
		framework.DumpJson();
	}

	[TestMethod]
	public void Debugging2()
	{
		var framework = TargetFrameworkService.GetOrAddFramework("net8.0-windows10.0.19041.0");
		var settings = new JsonSerializerSettings
		{
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			Formatting = Formatting.Indented
		};
		JsonConvert.SerializeObject(framework, settings).Dump();
	}

	[TestMethod]
	public void GenerateScenarios()
	{
		if (!EnableFileUpdates && !IsDebugging)
		{
			return;
		}

		var filePath = $@"{UnitTestsDirectory}\Parsers\VisualStudio\{nameof(TargetFrameworkMonikerTests)}.cs";
		var details = EnumExtensions.GetAllEnumDetails(typeof(TargetFrameworkType));
		var monikers = details.Select(x => x.Value.Name).ToList();
		monikers.Add("net6.0-ios15.0");
		monikers.Add("net6.0-android31.0");
		monikers.Add("net8.0-windows10.0.19041.0");
		monikers.Add("uap10.0.19041");
		monikers.Add("MonoAndroid12.0");
		monikers.Add("Xamarin.iOS10");

		var results = monikers
			.Select(TargetFrameworkService.GetOrAddFramework)
			.ToDictionary(x => x.Moniker, TargetFrameworkMoniker.ShallowClone);
		var code = CSharpCodeWriter.GenerateCode(results);
		code = code
			.Replace("new TargetFrameworkMoniker()", "new TargetFrameworkMoniker")
			.Replace("(int) ", string.Empty)
			.Insert(0, "return ");

		UpdateFileIfNecessary("// <StartGenerated>\r\n", "// </StartGenerated>", filePath, code + ";", "\t\t");
	}

	[TestMethod]
	public void GetBestNetStandard()
	{
		//AreEqual(TargetFrameworkType.NetStandard20,
		//	TargetFrameworkService.GetBestNetStandardFramework([
		//		TargetFrameworkService.ByType(TargetFrameworkType.NetStandard14),
		//		TargetFrameworkService.ByType(TargetFrameworkType.NetStandard20)
		//	]).FrameworkType
		//);

		//AreEqual(TargetFrameworkType.NetStandard21,
		//	TargetFrameworkService.GetBestNetStandardFramework([
		//		TargetFrameworkService.ByType(TargetFrameworkType.NetStandard12),
		//		TargetFrameworkService.ByType(TargetFrameworkType.NetStandard21),
		//		TargetFrameworkService.ByType(TargetFrameworkType.NetStandard20)
		//	]).FrameworkType
		//);
	}

	[TestMethod]
	public void RunScenarios()
	{
		var scenarios = GetScenarios();

		foreach (var scenario in scenarios)
		{
			var actual = TargetFrameworkService.GetOrAddFramework(scenario.Key);
			AreEqual(scenario.Value, actual);
		}
	}

	private Dictionary<string, TargetFrameworkMoniker> GetScenarios()
	{
		// <StartGenerated>
		return new Dictionary<string, TargetFrameworkMoniker>
		{
			{
				"Unknown",
				new TargetFrameworkMoniker
				{
					Moniker = "Unknown",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetStandard10",
				new TargetFrameworkMoniker
				{
					Moniker = "NetStandard10",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetStandard11",
				new TargetFrameworkMoniker
				{
					Moniker = "NetStandard11",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetStandard12",
				new TargetFrameworkMoniker
				{
					Moniker = "NetStandard12",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetStandard13",
				new TargetFrameworkMoniker
				{
					Moniker = "NetStandard13",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetStandard14",
				new TargetFrameworkMoniker
				{
					Moniker = "NetStandard14",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetStandard15",
				new TargetFrameworkMoniker
				{
					Moniker = "NetStandard15",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetStandard16",
				new TargetFrameworkMoniker
				{
					Moniker = "NetStandard16",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetStandard20",
				new TargetFrameworkMoniker
				{
					Moniker = "NetStandard20",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"net11",
				new TargetFrameworkMoniker
				{
					Moniker = "net11",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net20",
				new TargetFrameworkMoniker
				{
					Moniker = "net20",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net35",
				new TargetFrameworkMoniker
				{
					Moniker = "net35",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net40",
				new TargetFrameworkMoniker
				{
					Moniker = "net40",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net403",
				new TargetFrameworkMoniker
				{
					Moniker = "net403",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net45",
				new TargetFrameworkMoniker
				{
					Moniker = "net45",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net451",
				new TargetFrameworkMoniker
				{
					Moniker = "net451",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net452",
				new TargetFrameworkMoniker
				{
					Moniker = "net452",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net46",
				new TargetFrameworkMoniker
				{
					Moniker = "net46",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net461",
				new TargetFrameworkMoniker
				{
					Moniker = "net461",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net462",
				new TargetFrameworkMoniker
				{
					Moniker = "net462",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net463",
				new TargetFrameworkMoniker
				{
					Moniker = "net463",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net47",
				new TargetFrameworkMoniker
				{
					Moniker = "net47",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net471",
				new TargetFrameworkMoniker
				{
					Moniker = "net471",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net472",
				new TargetFrameworkMoniker
				{
					Moniker = "net472",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"net48",
				new TargetFrameworkMoniker
				{
					Moniker = "net48",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"uap",
				new TargetFrameworkMoniker
				{
					Moniker = "uap",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"Uap10",
				new TargetFrameworkMoniker
				{
					Moniker = "Uap10",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"netcore",
				new TargetFrameworkMoniker
				{
					Moniker = "netcore",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"netcore45",
				new TargetFrameworkMoniker
				{
					Moniker = "netcore45",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"netcore451",
				new TargetFrameworkMoniker
				{
					Moniker = "netcore451",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"netcore50",
				new TargetFrameworkMoniker
				{
					Moniker = "netcore50",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"NetCoreApp10",
				new TargetFrameworkMoniker
				{
					Moniker = "NetCoreApp10",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetCoreApp11",
				new TargetFrameworkMoniker
				{
					Moniker = "NetCoreApp11",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetCoreApp20",
				new TargetFrameworkMoniker
				{
					Moniker = "NetCoreApp20",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetCoreApp21",
				new TargetFrameworkMoniker
				{
					Moniker = "NetCoreApp21",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetCoreApp22",
				new TargetFrameworkMoniker
				{
					Moniker = "NetCoreApp22",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetStandard21",
				new TargetFrameworkMoniker
				{
					Moniker = "NetStandard21",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetCoreApp30",
				new TargetFrameworkMoniker
				{
					Moniker = "NetCoreApp30",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"NetCoreApp31",
				new TargetFrameworkMoniker
				{
					Moniker = "NetCoreApp31",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net5",
				new TargetFrameworkMoniker
				{
					Moniker = "Net5",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net5Windows",
				new TargetFrameworkMoniker
				{
					Moniker = "Net5Windows",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net6",
				new TargetFrameworkMoniker
				{
					Moniker = "Net6",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net6Android",
				new TargetFrameworkMoniker
				{
					Moniker = "Net6Android",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net6Ios",
				new TargetFrameworkMoniker
				{
					Moniker = "Net6Ios",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net6MacCatalyst",
				new TargetFrameworkMoniker
				{
					Moniker = "Net6MacCatalyst",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net6MacOs",
				new TargetFrameworkMoniker
				{
					Moniker = "Net6MacOs",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net6TvOs",
				new TargetFrameworkMoniker
				{
					Moniker = "Net6TvOs",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net6Windows",
				new TargetFrameworkMoniker
				{
					Moniker = "Net6Windows",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net7",
				new TargetFrameworkMoniker
				{
					Moniker = "Net7",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net7Android",
				new TargetFrameworkMoniker
				{
					Moniker = "Net7Android",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net7Ios",
				new TargetFrameworkMoniker
				{
					Moniker = "Net7Ios",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net7MacCatalyst",
				new TargetFrameworkMoniker
				{
					Moniker = "Net7MacCatalyst",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net7MacOs",
				new TargetFrameworkMoniker
				{
					Moniker = "Net7MacOs",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net7TvOs",
				new TargetFrameworkMoniker
				{
					Moniker = "Net7TvOs",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net7Windows",
				new TargetFrameworkMoniker
				{
					Moniker = "Net7Windows",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net8",
				new TargetFrameworkMoniker
				{
					Moniker = "Net8",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net8Android",
				new TargetFrameworkMoniker
				{
					Moniker = "Net8Android",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net8Browser",
				new TargetFrameworkMoniker
				{
					Moniker = "Net8Browser",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net8Ios",
				new TargetFrameworkMoniker
				{
					Moniker = "Net8Ios",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net8MacCatalyst",
				new TargetFrameworkMoniker
				{
					Moniker = "Net8MacCatalyst",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net8MacOs",
				new TargetFrameworkMoniker
				{
					Moniker = "Net8MacOs",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net8TvOs",
				new TargetFrameworkMoniker
				{
					Moniker = "Net8TvOs",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"Net8Windows",
				new TargetFrameworkMoniker
				{
					Moniker = "Net8Windows",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"MonoAndroid",
				new TargetFrameworkMoniker
				{
					Moniker = "MonoAndroid",
					OperatingSystem = "Android",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = null
				}
			},
			{
				"XamarinIos",
				new TargetFrameworkMoniker
				{
					Moniker = "XamarinIos",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"XamarinMac",
				new TargetFrameworkMoniker
				{
					Moniker = "XamarinMac",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"XamarinTvOs",
				new TargetFrameworkMoniker
				{
					Moniker = "XamarinTvOs",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"XamarinWatchOs",
				new TargetFrameworkMoniker
				{
					Moniker = "XamarinWatchOs",
					OperatingSystem = "",
					OperatingSystemVersion = "",
					PlatformMoniker = "",
					TypeMoniker = ""
				}
			},
			{
				"net6.0-ios15.0",
				new TargetFrameworkMoniker
				{
					Moniker = "net6.0-ios15.0",
					OperatingSystem = "ios",
					OperatingSystemVersion = "15.0",
					PlatformMoniker = "net6.0",
					TypeMoniker = "net6.0-ios"
				}
			},
			{
				"net6.0-android31.0",
				new TargetFrameworkMoniker
				{
					Moniker = "net6.0-android31.0",
					OperatingSystem = "android",
					OperatingSystemVersion = "31.0",
					PlatformMoniker = "net6.0",
					TypeMoniker = "net6.0-android"
				}
			},
			{
				"net8.0-windows10.0.19041.0",
				new TargetFrameworkMoniker
				{
					Moniker = "net8.0-windows10.0.19041.0",
					OperatingSystem = "windows",
					OperatingSystemVersion = "10.0.19041.0",
					PlatformMoniker = "net8.0",
					TypeMoniker = "net8.0-windows"
				}
			},
			{
				"uap10.0.19041",
				new TargetFrameworkMoniker
				{
					Moniker = "uap10.0.19041",
					OperatingSystem = "",
					OperatingSystemVersion = "10.0.19041",
					PlatformMoniker = "uap",
					TypeMoniker = "uap"
				}
			},
			{
				"MonoAndroid12.0",
				new TargetFrameworkMoniker
				{
					Moniker = "MonoAndroid12.0",
					OperatingSystem = "",
					OperatingSystemVersion = "12.0",
					PlatformMoniker = "MonoAndroid",
					TypeMoniker = "MonoAndroid"
				}
			},
			{
				"Xamarin.iOS10",
				new TargetFrameworkMoniker
				{
					Moniker = "Xamarin.iOS10",
					OperatingSystem = "",
					OperatingSystemVersion = "10",
					PlatformMoniker = "Xamarin.iOS",
					TypeMoniker = "Xamarin.iOS"
				}
			}
		};
		// </StartGenerated>
	}

	#endregion
}