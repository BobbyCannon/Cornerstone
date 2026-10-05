#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class VisualExtensionsTests
{
	#region Methods

	[PresentationTestMethod]
	public void FindAncestorOfTypeFindsAncestorOfNestedChild()
	{
		Button target;

		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					new StackPanel
					{
						Children =
						{
							(target = new Button())
						}
					}
				}
			}
		};

		CornerstoneTest.AreEqual(root, target.FindAncestorOfType<TestRoot>());
	}

	[PresentationTestMethod]
	public void FindAncestorOfTypeFindsDirectParent()
	{
		StackPanel target;

		var root = new TestRoot
		{
			Child = target = new StackPanel()
		};

		CornerstoneTest.AreEqual(root, target.FindAncestorOfType<TestRoot>());
	}

	[PresentationTestMethod]
	public void FindAncestorOfTypeFindsVisibleParent()
	{
		StackPanel target;

		var root = new TestRoot
		{
			Child = new TestRoot
			{
				Child = target = new StackPanel(),
				IsVisible = false
			}
		};

		CornerstoneTest.AreEqual(root, target.FindAncestorOfType<TestRoot>(false, v => v.IsVisible));
	}

	[PresentationTestMethod]
	public void FindCommonVisualAncestorFirstIsParentOfSecond()
	{
		Control left, right;

		var root = new TestRoot
		{
			Child = left = new Decorator
			{
				Child = right = new Decorator()
			}
		};

		var ancestor = left.FindCommonVisualAncestor(right);
		CornerstoneTest.AreEqual(left, ancestor);

		ancestor = right.FindCommonVisualAncestor(left);
		CornerstoneTest.AreEqual(left, ancestor);
	}

	[PresentationTestMethod]
	public void FindCommonVisualAncestorTwoSubtreesNonUniformHeight()
	{
		Control left, right;

		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					new Decorator
					{
						Child = new Decorator
						{
							Child = left = new Decorator()
						}
					},
					new Decorator
					{
						Child = new Decorator
						{
							Child = new Decorator
							{
								Child = right = new Decorator()
							}
						}
					}
				}
			}
		};

		var ancestor = left.FindCommonVisualAncestor(right);
		CornerstoneTest.AreEqual(root.Child, ancestor);

		ancestor = right.FindCommonVisualAncestor(left);
		CornerstoneTest.AreEqual(root.Child, ancestor);
	}

	[PresentationTestMethod]
	public void FindCommonVisualAncestorTwoSubtreesUniformHeight()
	{
		Control left, right;

		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					new Decorator
					{
						Child = new Decorator
						{
							Child = left = new Decorator()
						}
					},
					new Decorator
					{
						Child = new Decorator
						{
							Child = right = new Decorator()
						}
					}
				}
			}
		};

		var ancestor = left.FindCommonVisualAncestor(right);
		CornerstoneTest.AreEqual(root.Child, ancestor);

		ancestor = right.FindCommonVisualAncestor(left);
		CornerstoneTest.AreEqual(root.Child, ancestor);
	}

	[PresentationTestMethod]
	public void FindDescendantOfTypeFindsDirectChild()
	{
		StackPanel target;

		var root = new TestRoot
		{
			Child = target = new StackPanel()
		};

		CornerstoneTest.AreEqual(target, root.FindDescendantOfType<StackPanel>());
	}

	[PresentationTestMethod]
	public void FindDescendantOfTypeFindsNestedChild()
	{
		Button target;

		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					new StackPanel
					{
						Children =
						{
							(target = new Button())
						}
					}
				}
			}
		};

		CornerstoneTest.AreEqual(target, root.FindDescendantOfType<Button>());
	}

	[PresentationTestMethod]
	public void FindDescendantOfTypeFindsNestedVisibleChild()
	{
		Button target;

		var root = new TestRoot
		{
			Child = new StackPanel
			{
				Children =
				{
					new StackPanel
					{
						Children =
						{
							new Button { IsVisible = false },
							(target = new Button())
						}
					}
				}
			}
		};

		CornerstoneTest.AreEqual(target, root.FindDescendantOfType<Button>(false, v => v.IsVisible));
	}

	[PresentationTestMethod]
	public void TranslatePointShouldRespectRenderTransforms()
	{
		Border target;
		var root = new TestRoot
		{
			Width = 100,
			Height = 100,
			Child = new Decorator
			{
				Width = 50,
				Height = 50,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				RenderTransform = new TranslateTransform(25, 25),
				Child = target = new Border()
			}
		};

		root.Measure(Size.Infinity);
		root.Arrange(new Rect(root.DesiredSize));

		var result = target.TranslatePoint(new Point(0, 0), root);

		CornerstoneTest.AreEqual(new Point(50, 50), result);
	}

	#endregion
}