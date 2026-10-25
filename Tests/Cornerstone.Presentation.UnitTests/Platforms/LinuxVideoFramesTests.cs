#region References

using System;
using System.IO;
using System.Linq;
using Cornerstone.Presentation.Platforms.Desktop;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Platforms;

[TestClass]
public class LinuxVideoFramesTests
{
	#region Methods

	[TestMethod]
	public void H264PluginDirectoriesIncludeDebianAndArchLayouts()
	{
		var directories = LinuxVideoFrames.H264PluginDirectories();

		CornerstoneTest.IsTrue(directories.Contains("/usr/lib/x86_64-linux-gnu/vlc/plugins"));
		CornerstoneTest.IsTrue(directories.Contains("/usr/lib/aarch64-linux-gnu/vlc/plugins"));
		CornerstoneTest.IsTrue(directories.Contains("/usr/lib64/vlc/plugins"));
		CornerstoneTest.IsTrue(directories.Contains("/usr/lib/vlc/plugins"));
	}

	[TestMethod]
	public void HasH264PluginRequiresTheCodecFile()
	{
		var root = Path.Combine(Path.GetTempPath(), "cornerstone-vlc-" + Guid.NewGuid().ToString("N"));
		try
		{
			Directory.CreateDirectory(Path.Combine(root, "codec"));
			CornerstoneTest.IsFalse(LinuxVideoFrames.HasH264Plugin(root));
			CornerstoneTest.IsFalse(LinuxVideoFrames.HasH264Plugin(null));

			File.WriteAllBytes(Path.Combine(root, "codec", "libavcodec_plugin.so"), new byte[] { 0 });

			CornerstoneTest.IsTrue(LinuxVideoFrames.HasH264Plugin(root));
		}
		finally
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, true);
			}
		}
	}

	#endregion
}
