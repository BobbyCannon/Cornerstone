#region References

using Cornerstone.Avalonia.Converters;
using Cornerstone.Testing;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Converters;

[TestFixture]
public class StringValueConverterTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void Format()
	{
		var scenarios = new (object value, string format, string expected)[]
		{
			(47f, "X2", "2F"),
			(196.0, "X2", "C4"),
			(196.0, "0x{0:X4}", "0x00C4"),
			(128, "0x{0:X2}", "0x80"),
			(128, "X2", "80"),
			(26, "d4", "0026")
		};

		for (var index = 0; index < scenarios.Length; index++)
		{
			index.Dump();
			var scenario = scenarios[index];
			var converter = new StringValueConverter { Format = scenario.format };
			var actual = converter.Convert(scenario.value, typeof(string), null, null);
			AreEqual(scenario.expected, actual);
		}
	}

	#endregion
}