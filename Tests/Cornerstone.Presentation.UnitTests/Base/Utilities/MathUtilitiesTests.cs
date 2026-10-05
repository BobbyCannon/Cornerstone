#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

[TestClass]
public class MathUtilitiesTests
{
	#region Constants

	private const double AnyValue = 42.42;

	#endregion

	#region Fields

	private readonly double _calculatedAnyValue;
	private readonly double _one;
	private readonly double _zero;

	#endregion

	#region Constructors

	public MathUtilitiesTests()
	{
		_calculatedAnyValue = 0.0;
		_one = 0.0;
		_zero = 1.0;

		const int N = 10;
		var dxAny = AnyValue / N;
		var dxOne = 1.0 / N;
		var dxZero = _zero / N;

		for (var i = 0; i < N; ++i)
		{
			_calculatedAnyValue += dxAny;
			_one += dxOne;
			_zero -= dxZero;
		}
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void CalculatedDoubleOneIsOne()
	{
		var actual = MathUtilities.IsOne(_one);

		CornerstoneTest.IsTrue(actual);
		CornerstoneTest.AreEqual(1.0, Math.Round(_one, 15));
	}

	[PresentationTestMethod]
	public void CalculatedDoubleZeroIsZero()
	{
		var actual = MathUtilities.IsZero(_zero);

		CornerstoneTest.IsTrue(actual);
		CornerstoneTest.AreEqual(0.0, Math.Round(_zero, 15));
	}

	[PresentationTestMethod]
	public void CalculatedSingleOneIsOne()
	{
		var actualValue = (float) _one;

		var actual = MathUtilities.IsOne(actualValue);

		CornerstoneTest.IsTrue(actual);
		CornerstoneTest.AreEqual(1.0f, (float) Math.Round(actualValue, 7));
	}

	[PresentationTestMethod]
	public void CalculatedSingleZeroIsZero()
	{
		var actualValue = (float) _zero;

		var actual = MathUtilities.IsZero(actualValue);

		CornerstoneTest.IsTrue(actual);
		CornerstoneTest.AreEqual(0.0f, (float) Math.Round(actualValue, 7));
	}

	[PresentationTestMethod]
	public void DoubleFloatOneGreaterThanOrCloseOne()
	{
		var actual = MathUtilities.GreaterThanOrClose(1d, 1d);
		CornerstoneTest.IsTrue(actual);
	}

	[PresentationTestMethod]
	public void DoubleFloatOneGreaterThanZero()
	{
		var actual = MathUtilities.GreaterThan(1d, 0d);
		CornerstoneTest.IsTrue(actual);
	}

	[PresentationTestMethod]
	public void DoubleFloatOneLessThanOrCloseOne()
	{
		var actual = MathUtilities.LessThanOrClose(1d, 1d);
		CornerstoneTest.IsTrue(actual);
	}

	[PresentationTestMethod]
	public void DoubleFloatOneNotLessThanZero()
	{
		var actual = MathUtilities.LessThan(1d, 0d);
		CornerstoneTest.IsFalse(actual);
	}

	[PresentationTestMethod]
	public void DoubleFloatZeroLessThanOne()
	{
		var actual = MathUtilities.LessThan(0d, 1d);
		CornerstoneTest.IsTrue(actual);
	}

	[PresentationTestMethod]
	public void DoubleFloatZeroNotGreaterThanOne()
	{
		var actual = MathUtilities.GreaterThan(0d, 1d);
		CornerstoneTest.IsFalse(actual);
	}

	[PresentationTestMethod]
	public void FloatClampInputNaNReturnNaN()
	{
		var clamp = MathUtilities.Clamp(double.NaN, 0.0, 1.0);
		CornerstoneTest.IsTrue(double.IsNaN(clamp));
	}

	[PresentationTestMethod]
	public void FloatClampInputNegativeInfinityReturnMin()
	{
		const double min = 0.0;
		const double max = 1.0;
		var actual = MathUtilities.Clamp(double.NegativeInfinity, min, max);
		CornerstoneTest.AreEqual(min, actual);
	}

	[PresentationTestMethod]
	public void FloatClampInputPositiveInfinityReturnMax()
	{
		const double min = 0.0;
		const double max = 1.0;
		var actual = MathUtilities.Clamp(double.PositiveInfinity, min, max);
		CornerstoneTest.AreEqual(max, actual);
	}

	[PresentationTestMethod]
	public void SingleFloatOneGreaterThanOrCloseOne()
	{
		var actual = MathUtilities.GreaterThanOrClose(1f, 1f);
		CornerstoneTest.IsTrue(actual);
	}

	[PresentationTestMethod]
	public void SingleFloatOneGreaterThanZero()
	{
		var actual = MathUtilities.GreaterThan(1f, 0f);
		CornerstoneTest.IsTrue(actual);
	}

	[PresentationTestMethod]
	public void SingleFloatOneLessThanOrCloseOne()
	{
		var actual = MathUtilities.LessThanOrClose(1f, 1f);
		CornerstoneTest.IsTrue(actual);
	}

	[PresentationTestMethod]
	public void SingleFloatOneNotLessThanZero()
	{
		var actual = MathUtilities.LessThan(1f, 0f);
		CornerstoneTest.IsFalse(actual);
	}

	[PresentationTestMethod]
	public void SingleFloatZeroLessThanOne()
	{
		var actual = MathUtilities.LessThan(0f, 1f);
		CornerstoneTest.IsTrue(actual);
	}

	[PresentationTestMethod]
	public void SingleFloatZeroNotGreaterThanOne()
	{
		var actual = MathUtilities.GreaterThan(0f, 1f);
		CornerstoneTest.IsFalse(actual);
	}

	[PresentationTestMethod]
	public void TwoEquivalentDoubleValuesAreClose()
	{
		var actual = MathUtilities.AreClose(AnyValue, _calculatedAnyValue);

		CornerstoneTest.IsTrue(actual);
		CornerstoneTest.AreEqual(AnyValue, Math.Round(_calculatedAnyValue, 14));
	}

	[PresentationTestMethod]
	public void TwoEquivalentSingleValuesAreClose()
	{
		var expectedValue = (float) AnyValue;
		var actualValue = (float) _calculatedAnyValue;

		var actual = MathUtilities.AreClose(expectedValue, actualValue);

		CornerstoneTest.IsTrue(actual);
		CornerstoneTest.AreEqual((float) Math.Round(expectedValue, 5), (float) Math.Round(actualValue, 4));
	}

	#endregion
}