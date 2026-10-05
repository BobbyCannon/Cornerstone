#region References

using Cornerstone.VisualStudio.Core.Preview;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class WebProjectDetectionTests
{
	#region Methods

	[TestMethod]
	public void SignalRClientIsNotAWebServerReference()
	{
		Assert.IsFalse(WebProjectDetection.IsAspNetWebServerAssembly("Microsoft.AspNetCore.SignalR.Client"));
		Assert.IsFalse(WebProjectDetection.HasAspNetWebServerReferences(
			new[] { "Microsoft.AspNetCore.SignalR.Client" }));
	}

	[TestMethod]
	public void AspNetCoreAppIsAWebServerReference()
	{
		Assert.IsTrue(WebProjectDetection.IsAspNetWebServerAssembly("Microsoft.AspNetCore.App"));
		Assert.IsTrue(WebProjectDetection.HasAspNetWebServerReferences(
			new[] { "Microsoft.AspNetCore.Mvc" }));
	}

	[TestMethod]
	public void AlbumDesktopIsNotWebDespiteSignalRClient()
	{
		Assert.IsFalse(WebProjectDetection.IsLikelyWebProject(
			new[] { "Microsoft.AspNetCore.SignalR.Client" },
			usingWebSdk: false,
			hasDesktopPlatformOutput: true));
	}

	[TestMethod]
	public void WebSdkWithoutDesktopStackIsWeb()
	{
		Assert.IsTrue(WebProjectDetection.IsLikelyWebProject(
			new[] { "Microsoft.AspNetCore.App" },
			usingWebSdk: true,
			hasDesktopPlatformOutput: false));
	}

	[TestMethod]
	public void WebSdkWithAvaloniaDesktopIsNotWeb()
	{
		Assert.IsFalse(WebProjectDetection.IsLikelyWebProject(
			new[] { "Microsoft.AspNetCore.App", "Avalonia.Desktop" },
			usingWebSdk: true,
			hasDesktopPlatformOutput: false));
	}

	#endregion
}
