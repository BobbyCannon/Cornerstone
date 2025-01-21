#region References

using System.Globalization;
using Cornerstone.Avalonia.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Converters;

[TestClass]
public class DecimalConvertersTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void FromMillibarsToMercury()
	{
		AreEqual(29.259190246601911357496141217m,
			DecimalConverters.FromMillibarsToMercury.Convert(990.83m, typeof(decimal), null, CultureInfo.CurrentCulture)
		);
	}

	#endregion
}