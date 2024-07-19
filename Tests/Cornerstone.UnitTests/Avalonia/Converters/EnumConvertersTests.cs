#region References

using Cornerstone.Avalonia.Converters;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Converters;

[TestClass]
public class EnumConvertersTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ShouldMatchInt()
	{
		var eValue = DeviceType.Desktop;
		IsTrue(EnumConverters.Convert(eValue, 1));
		IsTrue(EnumConverters.Convert(eValue, "Desktop"));
	}

	#endregion
}