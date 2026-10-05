#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.VisualTree;

[TestClass]
public class VisualExtensionsGetTransformedBounds
{
	#region Methods

	[PresentationTestMethod]
	public void Depth1NoTransformOrClip()
	{
		Border target;
		var root = new Border
		{
			Width = 1000,
			Height = 1000,
			Child = target = new Border
			{
				Width = 500,
				Height = 500
			}
		};

		Layout(root);

		CornerstoneTest.AreEqual(new TransformedBounds(
			new Rect(0, 0, 500, 500),
			new Rect(0, 0, 1000, 1000),
			Matrix.CreateTranslation(250, 250)), target.GetTransformedBounds());
	}

	[PresentationTestMethod]
	public void Depth2NoTransformOrClip()
	{
		Border target;
		var root = new Border
		{
			Width = 1000,
			Height = 1000,
			Child = new Border
			{
				Width = 800,
				Height = 800,
				Child = target = new Border
				{
					Width = 500,
					Height = 500
				}
			}
		};

		Layout(root);

		CornerstoneTest.AreEqual(new TransformedBounds(
			new Rect(0, 0, 500, 500),
			new Rect(0, 0, 1000, 1000),
			Matrix.CreateTranslation(250, 250)), target.GetTransformedBounds());
	}

	[PresentationTestMethod]
	public void Depth2NoTransformWithClip()
	{
		Border target;
		var root = new Border
		{
			Width = 1000,
			Height = 1000,
			Child = new Border
			{
				Width = 800,
				Height = 800,
				ClipToBounds = true,
				Child = target = new Border
				{
					Width = 500,
					Height = 500
				}
			}
		};

		Layout(root);

		CornerstoneTest.AreEqual(new TransformedBounds(
			new Rect(0, 0, 500, 500),
			new Rect(100, 100, 800, 800),
			Matrix.CreateTranslation(250, 250)), target.GetTransformedBounds());
	}

	[PresentationTestMethod]
	public void Depth2TransformedClip()
	{
		Border target;
		var root = new Border
		{
			Width = 1000,
			Height = 1000,
			Child = new Border
			{
				Width = 800,
				Height = 800,
				ClipToBounds = true,
				RenderTransform = new MatrixTransform(Matrix.CreateTranslation(10, 20)),
				Child = target = new Border
				{
					Width = 500,
					Height = 500
				}
			}
		};

		Layout(root);

		CornerstoneTest.AreEqual(new TransformedBounds(
			new Rect(0, 0, 500, 500),
			new Rect(110, 120, 800, 800),
			Matrix.CreateTranslation(260, 270)), target.GetTransformedBounds());
	}

	[PresentationTestMethod]
	public void Root()
	{
		var root = new Border
		{
			Width = 100,
			Height = 123
		};

		Layout(root);

		CornerstoneTest.AreEqual(new TransformedBounds(
			new Rect(0, 0, 100, 123),
			new Rect(0, 0, 100, 123),
			Matrix.Identity), root.GetTransformedBounds());
	}

	private void Layout(Control c)
	{
		c.Measure(Size.Infinity);
		c.Arrange(new Rect(c.DesiredSize));
	}

	#endregion
}