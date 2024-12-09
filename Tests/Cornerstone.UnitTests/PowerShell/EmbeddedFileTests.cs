#region References

using Cornerstone.Extensions;
using Cornerstone.PowerShell;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.PowerShell;

[TestFixture]
public class EmbeddedFileTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void ReadEmbeddedFile()
	{
		var assembly = typeof(CmdletHelpGenerator).Assembly;
		assembly.ReadEmbeddedText("Cornerstone.PowerShell.Cornerstone.PowerShell.ps1xml");
		assembly.ReadEmbeddedText("Cornerstone.PowerShell.Cornerstone.PowerShell.psd1");
		assembly.ReadEmbeddedText("Cornerstone.PowerShell.Cornerstone.PowerShell-Help.xml");
	}

	#endregion
}