#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Transformation;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class TransformOperationsTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(0d, 10d)]
	[DataRow(0.5d, 15d)]
	[DataRow(1d, 20d)]
	public void CanInterpolateRotation(double progress, double angle)
	{
		var from = TransformOperations.Parse("rotate(10deg)");
		var to = TransformOperations.Parse("rotate(20deg)");

		var interpolated = TransformOperations.Interpolate(from, to, progress);

		var operations = interpolated.Operations;

		CornerstoneTest.Single(operations);
		CornerstoneTest.AreEqual(TransformOperation.OperationType.Rotate, operations[0].Type);
		CornerstoneTest.AreEqual(MathUtilities.Deg2Rad(angle), operations[0].Data.Rotate.Angle);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10d, 1d)]
	[DataRow(0.5d, 5.5d, 10.5d)]
	[DataRow(1d, 1d, 20d)]
	public void CanInterpolateScale(double progress, double x, double y)
	{
		var from = TransformOperations.Parse("scaleX(10)");
		var to = TransformOperations.Parse("scaleY(20)");

		var interpolated = TransformOperations.Interpolate(from, to, progress);

		var operations = interpolated.Operations;

		CornerstoneTest.Single(operations);
		CornerstoneTest.AreEqual(TransformOperation.OperationType.Scale, operations[0].Type);
		CornerstoneTest.AreEqual(x, operations[0].Data.Scale.X);
		CornerstoneTest.AreEqual(y, operations[0].Data.Scale.Y);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10d, 0d)]
	[DataRow(0.5d, 5d, 10d)]
	[DataRow(1d, 0d, 20d)]
	public void CanInterpolateSkew(double progress, double x, double y)
	{
		var from = TransformOperations.Parse("skewX(10deg)");
		var to = TransformOperations.Parse("skewY(20deg)");

		var interpolated = TransformOperations.Interpolate(from, to, progress);

		var operations = interpolated.Operations;

		CornerstoneTest.Single(operations);
		CornerstoneTest.AreEqual(TransformOperation.OperationType.Skew, operations[0].Type);
		CornerstoneTest.AreEqual(MathUtilities.Deg2Rad(x), operations[0].Data.Skew.X);
		CornerstoneTest.AreEqual(MathUtilities.Deg2Rad(y), operations[0].Data.Skew.Y);
	}

	[PresentationTestMethod]
	[DataRow(0d, 10d, 0d)]
	[DataRow(0.5d, 5d, 10d)]
	[DataRow(1d, 0d, 20d)]
	public void CanInterpolateTranslation(double progress, double x, double y)
	{
		var from = TransformOperations.Parse("translateX(10px)");
		var to = TransformOperations.Parse("translateY(20px)");

		var interpolated = TransformOperations.Interpolate(from, to, progress);

		var operations = interpolated.Operations;

		CornerstoneTest.Single(operations);
		CornerstoneTest.AreEqual(TransformOperation.OperationType.Translate, operations[0].Type);
		CornerstoneTest.AreEqual(x, operations[0].Data.Translate.X);
		CornerstoneTest.AreEqual(y, operations[0].Data.Translate.Y);
	}

	[PresentationTestMethod]
	public void CanParseCompoundOperations()
	{
		var data = "scale(1,2) translate(3px,4px) rotate(5deg) skew(6deg,7deg)";

		var transform = TransformOperations.Parse(data);

		var operations = transform.Operations;

		CornerstoneTest.AreEqual(TransformOperation.OperationType.Scale, operations[0].Type);
		CornerstoneTest.AreEqual(1, operations[0].Data.Scale.X);
		CornerstoneTest.AreEqual(2, operations[0].Data.Scale.Y);

		CornerstoneTest.AreEqual(TransformOperation.OperationType.Translate, operations[1].Type);
		CornerstoneTest.AreEqual(3, operations[1].Data.Translate.X);
		CornerstoneTest.AreEqual(4, operations[1].Data.Translate.Y);

		CornerstoneTest.AreEqual(TransformOperation.OperationType.Rotate, operations[2].Type);
		CornerstoneTest.AreEqual(MathUtilities.Deg2Rad(5), operations[2].Data.Rotate.Angle);

		CornerstoneTest.AreEqual(TransformOperation.OperationType.Skew, operations[3].Type);
		CornerstoneTest.AreEqual(MathUtilities.Deg2Rad(6), operations[3].Data.Skew.X);
		CornerstoneTest.AreEqual(MathUtilities.Deg2Rad(7), operations[3].Data.Skew.Y);
	}

	[PresentationTestMethod]
	public void CanParseMatrixOperation()
	{
		var data = "matrix(1,2,3,4,5,6)";

		var transform = TransformOperations.Parse(data);

		var operations = transform.Operations;

		CornerstoneTest.Single(operations);
		CornerstoneTest.AreEqual(TransformOperation.OperationType.Matrix, operations[0].Type);

		var expectedMatrix = new Matrix(1, 2, 3, 4, 5, 6);

		CornerstoneTest.AreEqual(expectedMatrix, operations[0].Matrix);
	}

	[PresentationTestMethod]
	[DataRow("rotate(90deg)", 90d)]
	[DataRow("rotate(0.5turn)", 180d)]
	[DataRow("rotate(200grad)", 180d)]
	[DataRow("rotate(3.14159265rad)", 180d)]
	public void CanParseRotation(string data, double angleDeg)
	{
		var transform = TransformOperations.Parse(data);

		var operations = transform.Operations;

		CornerstoneTest.Single(operations);
		CornerstoneTest.AreEqual(TransformOperation.OperationType.Rotate, operations[0].Type);
		CornerstoneTest.AreEqual(MathUtilities.Deg2Rad(angleDeg), operations[0].Data.Rotate.Angle, 4);
	}

	[PresentationTestMethod]
	[DataRow("scale(10)", 10d, 10d)]
	[DataRow("scale(10, 10)", 10d, 10d)]
	[DataRow("scale(0, 10)", 0d, 10d)]
	[DataRow("scale(10, 0)", 10d, 0d)]
	[DataRow("scaleX(10)", 10d, 1d)]
	[DataRow("scaleY(10)", 1d, 10d)]
	public void CanParseScale(string data, double x, double y)
	{
		var transform = TransformOperations.Parse(data);

		var operations = transform.Operations;

		CornerstoneTest.Single(operations);
		CornerstoneTest.AreEqual(TransformOperation.OperationType.Scale, operations[0].Type);
		CornerstoneTest.AreEqual(x, operations[0].Data.Scale.X);
		CornerstoneTest.AreEqual(y, operations[0].Data.Scale.Y);
	}

	[PresentationTestMethod]
	[DataRow("skew(90deg)", 90d, 0d)]
	[DataRow("skew(0.5turn)", 180d, 0d)]
	[DataRow("skew(200grad)", 180d, 0d)]
	[DataRow("skew(3.14159265rad)", 180d, 0d)]
	[DataRow("skewX(90deg)", 90d, 0d)]
	[DataRow("skewX(0.5turn)", 180d, 0d)]
	[DataRow("skewX(200grad)", 180d, 0d)]
	[DataRow("skewX(3.14159265rad)", 180d, 0d)]
	[DataRow("skew(0, 90deg)", 0d, 90d)]
	[DataRow("skew(0, 0.5turn)", 0d, 180d)]
	[DataRow("skew(0, 200grad)", 0d, 180d)]
	[DataRow("skew(0, 3.14159265rad)", 0d, 180d)]
	[DataRow("skewY(90deg)", 0d, 90d)]
	[DataRow("skewY(0.5turn)", 0d, 180d)]
	[DataRow("skewY(200grad)", 0d, 180d)]
	[DataRow("skewY(3.14159265rad)", 0d, 180d)]
	[DataRow("skew(90deg, 90deg)", 90d, 90d)]
	[DataRow("skew(0.5turn, 0.5turn)", 180d, 180d)]
	[DataRow("skew(200grad, 200grad)", 180d, 180d)]
	[DataRow("skew(3.14159265rad, 3.14159265rad)", 180d, 180d)]
	public void CanParseSkew(string data, double x, double y)
	{
		var transform = TransformOperations.Parse(data);

		var operations = transform.Operations;

		CornerstoneTest.Single(operations);
		CornerstoneTest.AreEqual(TransformOperation.OperationType.Skew, operations[0].Type);
		CornerstoneTest.AreEqual(MathUtilities.Deg2Rad(x), operations[0].Data.Skew.X, 4);
		CornerstoneTest.AreEqual(MathUtilities.Deg2Rad(y), operations[0].Data.Skew.Y, 4);
	}

	[PresentationTestMethod]
	[DataRow("translate(10px)", 10d, 0d)]
	[DataRow("translate(10px, 10px)", 10d, 10d)]
	[DataRow("translate(0px, 10px)", 0d, 10d)]
	[DataRow("translate(10px, 0px)", 10d, 0d)]
	[DataRow("translateX(10px)", 10d, 0d)]
	[DataRow("translateY(10px)", 0d, 10d)]
	public void CanParseTranslation(string data, double x, double y)
	{
		var transform = TransformOperations.Parse(data);

		var operations = transform.Operations;

		CornerstoneTest.Single(operations);
		CornerstoneTest.AreEqual(TransformOperation.OperationType.Translate, operations[0].Type);
		CornerstoneTest.AreEqual(x, operations[0].Data.Translate.X);
		CornerstoneTest.AreEqual(y, operations[0].Data.Translate.Y);
	}

	[PresentationTestMethod]
	public void InterpolationFallbackToMatrix()
	{
		var progress = 0.5d;

		var from = TransformOperations.Parse("rotate(45deg)");
		var to = TransformOperations.Parse("translate(100px, 100px) rotate(1215deg)");

		var interpolated = TransformOperations.Interpolate(from, to, progress);

		var operations = interpolated.Operations;

		CornerstoneTest.Single(operations);
		CornerstoneTest.AreEqual(TransformOperation.OperationType.Matrix, operations[0].Type);
	}

	[PresentationTestMethod]
	public void OrderOfOperationsIsPreservedNoPrefix()
	{
		var from = TransformOperations.Parse("scale(1)");
		var to = TransformOperations.Parse("translate(50px,50px) scale(0.5,0.5)");

		var interpolated0 = TransformOperations.Interpolate(from, to, 0);

		CornerstoneTest.IsTrue(interpolated0.IsIdentity);

		var interpolated50 = TransformOperations.Interpolate(from, to, 0.5);

		AssertMatrix(interpolated50.Value, scaleX: 0.75, scaleY: 0.75, translateX: 12.5, translateY: 12.5);

		var interpolated100 = TransformOperations.Interpolate(from, to, 1);

		AssertMatrix(interpolated100.Value, scaleX: 0.5, scaleY: 0.5, translateX: 25, translateY: 25);
	}

	[PresentationTestMethod]
	public void OrderOfOperationsIsPreservedOnePrefix()
	{
		var from = TransformOperations.Parse("scale(1)");
		var to = TransformOperations.Parse("scale(0.5,0.5) translate(50px,50px)");

		var interpolated0 = TransformOperations.Interpolate(from, to, 0);

		CornerstoneTest.IsTrue(interpolated0.IsIdentity);

		var interpolated50 = TransformOperations.Interpolate(from, to, 0.5);

		AssertMatrix(interpolated50.Value, scaleX: 0.75, scaleY: 0.75, translateX: 25.0, translateY: 25);

		var interpolated100 = TransformOperations.Interpolate(from, to, 1);

		AssertMatrix(interpolated100.Value, scaleX: 0.5, scaleY: 0.5, translateX: 50, translateY: 50);
	}

	[PresentationTestMethod]
	public void TransformGroupInvalidatesWhenChildCollectionChanges()
	{
		var group = new TransformGroup();
		var transform = new TranslateTransform(10, 0);

		CornerstoneTest.AreEqual(Matrix.Identity, group.Value);

		group.Children.Add(transform);

		CornerstoneTest.AreNotEqual(Matrix.Identity, group.Value);

		group.Children.Clear();

		CornerstoneTest.AreEqual(Matrix.Identity, group.Value);

		group.Children = [transform];

		CornerstoneTest.AreNotEqual(Matrix.Identity, group.Value);
	}

	private static void AssertMatrix(Matrix matrix, double? angle = null, double? scaleX = null, double? scaleY = null, double? translateX = null, double? translateY = null)
	{
		CornerstoneTest.IsTrue(Matrix.TryDecomposeTransform(matrix, out var composed));

		if (angle.HasValue)
		{
			CornerstoneTest.AreEqual(angle.Value, composed.Angle);
		}

		if (scaleX.HasValue)
		{
			CornerstoneTest.AreEqual(scaleX.Value, composed.Scale.X);
		}

		if (scaleY.HasValue)
		{
			CornerstoneTest.AreEqual(scaleY.Value, composed.Scale.Y);
		}

		if (translateX.HasValue)
		{
			CornerstoneTest.AreEqual(translateX.Value, composed.Translate.X);
		}

		if (translateY.HasValue)
		{
			CornerstoneTest.AreEqual(translateY.Value, composed.Translate.Y);
		}
	}

	#endregion
}