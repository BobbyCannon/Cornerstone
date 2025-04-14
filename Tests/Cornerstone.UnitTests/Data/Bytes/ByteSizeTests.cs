#region References

using System.Collections.Generic;
using Cornerstone.Data.Bytes;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data.Bytes;

[TestClass]
public class ByteSizeTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void NoLoss()
	{
		AreEqual(1.0m, ByteSize.FromBits(ByteSize.BitsInByte).Bytes);
		AreEqual(1.0m, ByteSize.FromBits(ByteSize.BitsInKilobit).Kilobits);
		AreEqual(1.0m, ByteSize.FromBits(ByteSize.BitsInKilobyte).Kilobytes);
		AreEqual(1.0m, ByteSize.FromBits(ByteSize.BitsInMegabit).Megabits);
		AreEqual(1.0m, ByteSize.FromBits(ByteSize.BitsInMegabyte).Megabytes);
		AreEqual(1.0m, ByteSize.FromBits(ByteSize.BitsInGigabit).Gigabits);
		AreEqual(1.0m, ByteSize.FromBits(ByteSize.BitsInGigabyte).Gigabytes);
	}

	[TestMethod]
	public void Constructor()
	{
		var scenarios = new List<(ByteSize expected, long bits, double bytes, double kilobits, double kilobytes, double megabits, double megabytes, double gigabits, double gigabytes, double terabits, double terabytes)>
		{
			(ByteSize.FromBits(1), 1, 0.125, 0.001, 0.0001220703125, 0.000001, 0.00000011920928955078125, 0.000000001, 0.0000000001164153218269348145, 0.000000000001, 0.000000000000113686837721616),
			(ByteSize.FromBytes(1), 8, 1, 0.008, 0.0009765625, 0.000008, 0.00000095367431640625, 0.000000008, 0.0000000009313225746154785156, 0.000000000008, 0.0000000000009094947017729282),
			(ByteSize.FromBytes(8), 64, 8, 0.064, 0.0078125, 0.000064, 0.00000762939453125, 0.000000064, 0.000000007450580596923828125, 0.000000000064, 0.0000000000072759576141834259),
			(ByteSize.FromKilobytes(1), 8192, 1024, 8.192, 1, 0.008192, 0.0009765625, 0.000008192, 0.00000095367431640625, 0.000000008192, 0.0000000009313225746154785156),
			(ByteSize.FromKilobytes(4), 32768, 4096, 32.768, 4, 0.032768, 0.00390625, 0.000032768, 0.000003814697265625, 0.000000032768, 0.0000000037252902984619140625),
			(ByteSize.FromMegabytes(1), 8388608, 1048576, 8388.608, 1024, 8.388608, 1, 0.008388608, 0.0009765625, 0.000008388608, 0.00000095367431640625),
			(ByteSize.FromMegabytes(5), 41943040, 5242880, 41943.04, 5120, 41.94304, 5, 0.04194304, 0.0048828125, 0.00004194304, 0.00000476837158203125),
			(ByteSize.FromGigabytes(1), 8589934592, 1073741824, 8589934.592, 1048576, 8589.934592, 1024, 8.589934592, 1, 0.008589934592, 0.0009765625),
			(ByteSize.FromGigabytes(6), 51539607552, 6442450944, 51539607.552, 6291456, 51539.607552, 6144, 51.539607552, 6, 0.051539607552, 0.005859375),
			(ByteSize.FromTerabytes(1), 8796093022208, 1099511627776, 8796093022.208, 1073741824, 8796093.022208, 1048576, 8796.093022208, 1024, 8.796093022208, 1),
			(ByteSize.FromTerabytes(7), 61572651155456, 7696581394432, 61572651155.456, 7516192768, 61572651.155456, 7340032, 61572.651155456, 7168, 61.572651155456, 7)
		};

		foreach (var scenario in scenarios)
		{
			var symbol = scenario.expected.GetLargestWholeNumberFullWord();

			if (IsDebugging)
			{
				CopyToClipboard(
					string.Format("({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}),",
						$"ByteSize.From{(symbol.EndsWith("s") ? symbol : symbol + "s")}({scenario.expected.LargestWholeNumberValue})",
						scenario.expected.Bits,
						scenario.expected.Bytes,
						scenario.expected.Kilobits,
						scenario.expected.Kilobytes,
						scenario.expected.Megabits,
						scenario.expected.Megabytes,
						scenario.expected.Gigabits,
						scenario.expected.Gigabytes,
						scenario.expected.Terabits,
						scenario.expected.Terabytes
					)
				).Dump();
			}

			AreEqual(scenario.bits, scenario.expected.Bits);
			AreEqual(scenario.bytes, scenario.expected.Bytes);
			AreEqual(scenario.kilobits, scenario.expected.Kilobits);
			AreEqual(scenario.kilobytes, scenario.expected.Kilobytes);
			AreEqual(scenario.megabits, scenario.expected.Megabits);
			AreEqual(scenario.megabytes, scenario.expected.Megabytes);
			AreEqual(scenario.gigabits, scenario.expected.Gigabits);
			AreEqual(scenario.gigabytes, scenario.expected.Gigabytes);
			AreEqual(scenario.terabits, scenario.expected.Terabits);
			AreEqual(scenario.terabytes, scenario.expected.Terabytes);
		}
	}

	[TestMethod]
	public void ToDifferentSize()
	{
		var scenarios = new Dictionary<string, string>
		{
			{ "0.13 Byte", ByteSize.FromBits(1).ToUnitString(ByteUnit.Byte) },
			{ "1 Byte", ByteSize.FromBits(8).ToUnitString(ByteUnit.Byte) },
			{ "2 Bytes", ByteSize.FromBits(16).ToUnitString(ByteUnit.Byte) }
		};

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Key, scenario.Value);
		}
	}

	[TestMethod]
	public void ToStrings()
	{
		var scenarios = new Dictionary<string, ByteSize>
		{
			{ "1 b", ByteSize.FromBits(1) },
			{ "1 B", ByteSize.FromBytes(1) },
			{ "1 KB", ByteSize.FromKilobytes(1) },
			{ "1 MB", ByteSize.FromMegabytes(1) },
			{ "1 GB", ByteSize.FromGigabytes(1) },
			{ "1 TB", ByteSize.FromTerabytes(1) }
		};

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Key, scenario.Value.ToString());
		}

		AreEqual("2 B", ByteSize.FromBits(16).ToString());
		AreEqual("1.5 B", ByteSize.FromBits(12).ToString());
		AreEqual("1000 B", ByteSize.FromBytes(1000).ToString());
		AreEqual("1 KB", ByteSize.FromBytes(1024).ToString());
	}

	[TestMethod]
	public void ToStringsFormats()
	{
		AreEqual("2 KB", ByteSize.FromBytes(2056).ToString("0.#"));
		AreEqual("2.1 KB", ByteSize.FromBytes(2170).ToString("0.#"));
		AreEqual("2.12 KB", ByteSize.FromBytes(2170).ToString("0.##"));
		AreEqual("2.119 KB", ByteSize.FromBytes(2170).ToString("0.###"));
	}

	#endregion
}