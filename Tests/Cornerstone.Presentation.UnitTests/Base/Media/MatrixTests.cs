#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class MatrixTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(30d)]
	[DataRow(0d)]
	[DataRow(90d)]
	[DataRow(270d)]
	public void CanDecomposeAngle(double angleDeg)
	{
		var angleRad = MathUtilities.Deg2Rad(angleDeg);

		var matrix = Matrix.CreateRotation(angleRad);

		var result = Matrix.TryDecomposeTransform(matrix, out var decomposed);

		CornerstoneTest.AreEqual(true, result);

		var expected = NormalizeAngle(angleRad);
		var actual = NormalizeAngle(decomposed.Angle);

		CornerstoneTest.AreEqual(expected, actual, 4);
	}

	[PresentationTestMethod]
	[DataRow(1d, 1d)]
	[DataRow(-1d, 1d)]
	[DataRow(1d, -1d)]
	[DataRow(5d, 10d)]
	public void CanDecomposeScale(double x, double y)
	{
		var matrix = Matrix.CreateScale(x, y);

		var result = Matrix.TryDecomposeTransform(matrix, out var decomposed);

		CornerstoneTest.AreEqual(true, result);
		CornerstoneTest.AreEqual(x, decomposed.Scale.X);
		CornerstoneTest.AreEqual(y, decomposed.Scale.Y);
	}

	[PresentationTestMethod]
	public void CanDecomposeTranslation()
	{
		var matrix = Matrix.CreateTranslation(5, 10);

		var result = Matrix.TryDecomposeTransform(matrix, out var decomposed);

		CornerstoneTest.AreEqual(true, result);
		CornerstoneTest.AreEqual(5, decomposed.Translate.X);
		CornerstoneTest.AreEqual(10, decomposed.Translate.Y);
	}

	[PresentationTestMethod]
	public void CanParse()
	{
		var matrix = Matrix.Parse("1,2,3,-4,5 6");
		var expected = new Matrix(1, 2, 3, -4, 5, 6);
		CornerstoneTest.AreEqual(expected, matrix);
	}

	[PresentationTestMethod]
	public void IdentityHasInverse()
	{
		var matrix = Matrix.Identity;

		CornerstoneTest.IsTrue(matrix.HasInverse);
	}

	[PresentationTestMethod]
	public void InvertShouldWork()
	{
		var matrix = new Matrix(1, 2, 3, 0, 1, 4, 5, 6, 0);
		var inverted = matrix.Invert();

		CornerstoneTest.AreEqual(matrix * inverted, Matrix.Identity);
		CornerstoneTest.AreEqual(inverted * matrix, Matrix.Identity);
	}

	[PresentationTestMethod]
	public void SingularHasNoInverse()
	{
		var matrix = new Matrix(0, 0, 0, 0, 0, 0);

		CornerstoneTest.IsFalse(matrix.HasInverse);
	}

	private static double NormalizeAngle(double rad)
	{
		var twoPi = 2 * Math.PI;

		while (rad < 0)
		{
			rad += twoPi;
		}

		while (rad > twoPi)
		{
			rad -= twoPi;
		}

		return rad;
	}

	#endregion
}