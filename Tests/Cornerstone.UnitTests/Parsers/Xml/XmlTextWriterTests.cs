#region References

using Cornerstone.Parsers.Xml;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Xml;

[TestClass]
public class XmlTextWriterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void EscapeString()
	{
		var input = @"powershell -NonInteractive -Command ""& '$(ProjectDir)\Invoke-PostBuild.ps1' -Path \""$(ProjectDir)\""""";
		var expected = @"powershell -NonInteractive -Command &quot;&amp; '$(ProjectDir)\Invoke-PostBuild.ps1' -Path \&quot;$(ProjectDir)\&quot;&quot;";
		var actual = new TextBuilder();
		XmlTextBuilder.AppendEscapedString(input, actual, '"');
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void ValidateQuotes()
	{
		AreEqual("mkdir .\\bin\\publish&#xD;&#xA;copy Install.bat .\\bin\\publish\\ /Y&#xD;&#xA;copy Uninstall.bat .\\bin\\publish\\ /Y",
			XmlTextBuilder.EscapedString("mkdir .\\bin\\publish\r\ncopy Install.bat .\\bin\\publish\\ /Y\r\ncopy Uninstall.bat .\\bin\\publish\\ /Y", '"')
		);
		AreEqual("'test'", XmlTextBuilder.EscapedString("'test'", '"'));
		AreEqual("'foo &quot;Bar&quot;'", XmlTextBuilder.EscapedString("'foo \"Bar\"'", '"'));
		AreEqual("&apos;foo \"Bar\"&apos;", XmlTextBuilder.EscapedString("'foo \"Bar\"'", '\''));
		AreEqual("'foo &quot;Bar &apos;World&apos;&quot;'",
			XmlTextBuilder.EscapedString("'foo \"Bar 'World'\"'", '"')
		);
		AreEqual("'foo' &quot;hello&quot; 'bar' &quot;world&quot; ''",
			XmlTextBuilder.EscapedString("'foo' \"hello\" 'bar' \"world\" ''", '"')
		);
	}

	#endregion
}