#region References

using Cornerstone.RemoteLink.Android;
using Cornerstone.RemoteLink.Android.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.RemoteLink;

[TestClass]
public class AndroidAdbTextTests : RemoteLinkUnitTest
{
	#region Methods

	[TestMethod]
	public void AlignOrientationSwapsWhenFrameIsRotated()
	{
		var width = 1080;
		var height = 2400;
		AndroidAdbText.AlignOrientation(960, 432, ref width, ref height);
		AreEqual(2400, width);
		AreEqual(1080, height);
	}

	[TestMethod]
	public void HelloFrameRoundTrip()
	{
		var bytes = RemoteLinkFraming.EncodeHello(1080, 2400, 0);
		AreEqual(RemoteLinkProtocol.Magic[0], bytes[0]);
		AreEqual((byte) RemoteLinkProtocol.MessageType.Hello, bytes[6]);
	}

	[TestMethod]
	public void ParseDevicesReadsModelAndState()
	{
		var output = """
					List of devices attached
					emulator-5554          device product:sdk_gphone64_x86_64 model:sdk_gphone64_x86_64 device:emu64xa
					ABC123 unauthorized
					""";
		var devices = AndroidAdbText.ParseDevices(output);
		AreEqual(2, devices.Count);
		AreEqual("emulator-5554", devices[0].Serial);
		AreEqual("device", devices[0].State);
		AreEqual("sdk gphone64 x86 64", devices[0].Model);
		AreEqual("ABC123", devices[1].Serial);
		AreEqual("unauthorized", devices[1].State);
	}

	[TestMethod]
	public void ParseWmSizePrefersOverrideSize()
	{
		IsTrue(AndroidAdbText.TryParseWmSize("Physical size: 1440x3120\nOverride size: 1080x2340\n", out var width, out var height));
		AreEqual(1080, width);
		AreEqual(2340, height);
	}

	[TestMethod]
	public void ParseWmSizeReadsPhysicalSize()
	{
		IsTrue(AndroidAdbText.TryParseWmSize("Physical size: 1080x2400\n", out var width, out var height));
		AreEqual(1080, width);
		AreEqual(2400, height);
	}

	#endregion
}