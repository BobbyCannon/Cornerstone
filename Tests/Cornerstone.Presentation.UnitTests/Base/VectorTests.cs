#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class VectorTests
{
	#region Methods

	[PresentationTestMethod]
	public void CrossShouldReturnCorrectValue()
	{
		var a = new Vector(-6, 8.0);
		var b = new Vector(5, 12.0);

		CornerstoneTest.AreEqual(-112.0, Vector.Cross(a, b));
	}

	[PresentationTestMethod]
	public void DiviedByVectorShouldReturnCorrectValue()
	{
		var a = new Vector(10, 2);
		var b = new Vector(5, 2);

		var expected = new Vector(2, 1);

		CornerstoneTest.AreEqual(expected, Vector.Divide(a, b));
	}

	[PresentationTestMethod]
	public void DiviedShouldReturnCorrectValue()
	{
		var vector = new Vector(10, 2);
		var expected = new Vector(5, 1);

		CornerstoneTest.AreEqual(expected, Vector.Divide(vector, 2));
	}

	[PresentationTestMethod]
	public void DotShouldReturnCorrectValue()
	{
		var a = new Vector(-6, 8.0);
		var b = new Vector(5, 12.0);

		CornerstoneTest.AreEqual(66.0, Vector.Dot(a, b));
	}

	[PresentationTestMethod]
	public void LengthShouldReturnCorrectLengthOfVector()
	{
		var vector = new Vector(2, 4);
		var length = Math.Sqrt((2 * 2) + (4 * 4));

		CornerstoneTest.AreEqual(length, vector.Length);
	}

	[PresentationTestMethod]
	public void LengthSquaredShouldReturnCorrectLengthOfVector()
	{
		var vectorA = new Vector(2, 4);
		var squaredLengthA = (2 * 2) + (4 * 4);

		CornerstoneTest.AreEqual(squaredLengthA, vectorA.SquaredLength);
	}

	[PresentationTestMethod]
	public void MultiplyByVectorShouldReturnCorrectValue()
	{
		var a = new Vector(10, 2);
		var b = new Vector(2, 2);

		var expected = new Vector(20, 4);

		CornerstoneTest.AreEqual(expected, Vector.Multiply(a, b));
	}

	[PresentationTestMethod]
	public void MultiplyShouldReturnCorrectValue()
	{
		var vector = new Vector(10, 2);

		var expected = new Vector(20, 4);

		CornerstoneTest.AreEqual(expected, Vector.Multiply(vector, 2));
	}

	[PresentationTestMethod]
	public void NegateShouldReturnNegatedVector()
	{
		var vector = new Vector(2, 4);
		var negated = new Vector(-2, -4);

		CornerstoneTest.AreEqual(negated, vector.Negate());
	}

	[PresentationTestMethod]
	public void NormalizeShouldReturnNormalizedVector()
	{
		// the length of a normalized vector must be 1

		var vectorA = new Vector(13, 84);
		var vectorB = new Vector(-34, 345);
		var vectorC = new Vector(-34, -84);

		CornerstoneTest.AreEqual(1.0, vectorA.Normalize().Length);
		CornerstoneTest.AreEqual(1.0, vectorB.Normalize().Length);
		CornerstoneTest.AreEqual(1.0, vectorC.Normalize().Length);
	}

	[PresentationTestMethod]
	public void ScaleVectorShouldBeCommutative()
	{
		var vector = new Vector(10, 2);

		var expected = vector * 2;

		CornerstoneTest.AreEqual(expected, 2 * vector);
	}

	#endregion
}