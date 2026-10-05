#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Data.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Converters;

/// <summary>
/// Proves the TabControlsButtons / NumericUpDown overflow: binding double (Height/Width, often NaN when unset)
/// into NumericUpDown.Value (decimal) throws OverflowException without a safe converter.
/// </summary>
[TestClass]
public class DoubleToDecimalConverterTests
{
	#region Methods

	[TestMethod]
	public void CastingInfinityDoubleToDecimalThrowsOverflowException()
	{
		var posInf = double.PositiveInfinity;
		var negInf = double.NegativeInfinity;
		Assert.ThrowsExactly<OverflowException>(() => _ = (decimal) posInf);
		Assert.ThrowsExactly<OverflowException>(() => _ = (decimal) negInf);
	}

	[TestMethod]
	public void CastingNaNDoubleToDecimalThrowsOverflowException()
	{
		// Exact failure mode reported when opening TabControlsButtons (Height/Width unset = double.NaN).
		// Use a local so the cast is runtime (compile-time (decimal)double.NaN is illegal).
		var nan = double.NaN;
		var ex = Assert.ThrowsExactly<OverflowException>(() => _ = (decimal) nan);
		StringAssert.Contains(ex.Message, "too large or too small for a Decimal");
	}

	[TestMethod]
	public void ConvertToDecimalNaNThrowsSameOverflow()
	{
		// Cornerstone binding coercion uses System.Convert.ToDecimal for double → decimal.
		var nan = double.NaN;
		var ex = Assert.ThrowsExactly<OverflowException>(() => _ = System.Convert.ToDecimal(nan));
		StringAssert.Contains(ex.Message, "too large or too small for a Decimal");
	}

	[TestMethod]
	public void DoubleToDecimalConverterDoesNotThrowOnNaNOrInfinity()
	{
		var converter = new DoubleToDecimalConverter();
		var culture = CultureInfo.InvariantCulture;

		Assert.AreEqual(0m, converter.Convert(double.NaN, typeof(decimal), null, culture));
		Assert.AreEqual(0m, converter.Convert(double.PositiveInfinity, typeof(decimal), null, culture));
		Assert.AreEqual(0m, converter.Convert(double.NegativeInfinity, typeof(decimal), null, culture));
		Assert.AreEqual(32m, converter.Convert(32.0, typeof(decimal), null, culture));
		Assert.AreEqual(120m, converter.Convert(120.0, typeof(decimal), null, culture));
	}

	[TestMethod]
	public void DoubleToDecimalConverterRoundTripsFiniteValues()
	{
		var converter = new DoubleToDecimalConverter();
		var culture = CultureInfo.InvariantCulture;

		var converted = converter.Convert(32.5, typeof(decimal), null, culture);
		Assert.AreEqual(32.5m, converted);
		Assert.AreEqual(32.5, converter.ConvertBack(converted, typeof(double), null, culture));
	}

	#endregion
}