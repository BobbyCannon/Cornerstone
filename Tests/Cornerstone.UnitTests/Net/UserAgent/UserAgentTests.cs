#region References

using System.Collections.Generic;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Net.UserAgent;

[TestClass]
public class UserAgentTests : CornerstoneUnitTest
{
	#region Methods

	/// <summary>
	/// Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36
	/// Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36 OPR/38.0.2220.41
	/// Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.59
	/// Mozilla/5.0 (compatible; MSIE 9.0; Windows Phone OS 7.5; Trident/5.0; IEMobile/9.0)
	/// Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)
	/// PostmanRuntime/7.26.5
	/// </summary>
	[TestMethod]
	public void ParseAgent()
	{
		var scenarios = new Dictionary<string, (Cornerstone.Net.UserAgent.UserAgent, string)>
		{
			{
				"Mozilla/5.0 (iPhone; CPU iPhone OS 13_5_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1.1 Mobile/15E148 Safari/604.1",
				(
					new Cornerstone.Net.UserAgent.UserAgent
					{
						Name = "Safari",
						Version = "604.1"
					},
					"Safari 604.1"
				)
			},
			{
				"curl/7.64.1",
				(
					new Cornerstone.Net.UserAgent.UserAgent
					{
						Name = "curl",
						Version = "7.64.1"
					},
					"curl 7.64.1"
				)
			},
			{
				"Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36",
				(
					new Cornerstone.Net.UserAgent.UserAgent
					{
						Name = "Safari",
						Version = "537.36",
						Bitness = "x86_64",
						DeviceType = DeviceType.Desktop,
						OperatingSystem = DevicePlatform.Linux
					},
					"Safari 537.36 Linux x86_64"
				)
			},
			{
				"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.59",
				(
					new Cornerstone.Net.UserAgent.UserAgent
					{
						Name = "Edge",
						Version = "91.0.864.59",
						Bitness = "x64",
						DeviceType = DeviceType.Desktop,
						OperatingSystem = DevicePlatform.Windows,
						OperatingSystemVersion = "10.0"
					},
					"Edge 91.0.864.59 Windows 10.0 x64"
				)
			},
			{
				"Mozilla/5.0 (Linux; Android 13; Pixel 4a) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Mobile Safari/537.36 EdgA/110.0.1587.66",
				(new Cornerstone.Net.UserAgent.UserAgent
					{
						Name = "Edge",
						Version = "110.0.1587.66",
						Device = "Pixel 4a",
						DeviceType = DeviceType.Phone,
						OperatingSystem = DevicePlatform.Android,
						OperatingSystemVersion = "13"
					},
					"Edge 110.0.1587.66 Pixel 4a Android 13"
				)
			},
			{
				"Mozilla/5.0 (Linux; Android 11; KFRAWI) AppleWebKit/537.36 (KHTML, like Gecko) Silk/110.2.5 like Chrome/110.0.5481.154 Safari/537.36",
				(
					new Cornerstone.Net.UserAgent.UserAgent
					{
						Name = "Safari",
						Version = "537.36",
						Device = "Amazon Fire HD 8 (2022)",
						DeviceType = DeviceType.Tablet,
						OperatingSystem = DevicePlatform.Android,
						OperatingSystemVersion = "11"
					},
					"Safari 537.36 Amazon Fire HD 8 (2022) Android 11"
				)
			}
		};

		foreach (var scenario in scenarios)
		{
			var actual = Cornerstone.Net.UserAgent.UserAgent.Parse(scenario.Key);
			AreEqual(scenario.Key, actual.ToString());
			AreEqual(scenario.Value.Item1, actual);
			AreEqual(scenario.Value.Item2, actual.ToSummary());
		}
	}

	#endregion
}