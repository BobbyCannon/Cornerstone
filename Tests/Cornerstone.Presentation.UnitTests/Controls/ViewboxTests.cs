#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ViewboxTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ChangingChildShouldInvalidateLayout()
	{
		var target = new Viewbox();

		target.Child = new Canvas
		{
			Width = 100,
			Height = 100
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));
		CornerstoneTest.AreEqual(new Size(100, 100), target.DesiredSize);

		target.Child = new Canvas
		{
			Width = 200,
			Height = 200
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));
		CornerstoneTest.AreEqual(new Size(200, 200), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void ChildDataContextBindingWorks()
	{
		var data = new
		{
			Foo = "foo"
		};

		var target = new Viewbox
		{
			DataContext = data,
			Child = new Canvas
			{
				[!Canvas.DataContextProperty] = new Binding("Foo")
			}
		};

		CornerstoneTest.AreEqual("foo", target.Child.DataContext);
	}

	[PresentationTestMethod]
	public void ChildShouldBeLogicalChildOfViewbox()
	{
		var target = new Viewbox();

		CornerstoneTest.Empty(target.GetLogicalChildren());

		var child = new Canvas();
		target.Child = child;

		CornerstoneTest.Single(target.GetLogicalChildren(), child);
		CornerstoneTest.Same(child.GetLogicalParent(), target);

		target.Child = null;

		CornerstoneTest.Empty(target.GetLogicalChildren());
		CornerstoneTest.IsNull(child.GetLogicalParent());
	}

	[PresentationTestMethod]
	public void ContentPresentedInViewboxShouldBeReparentedWhenTemplateChanges()
	{
		// Issue #9551: a template containing Viewbox > ContentPresenter presenting a control
		// that outlives the template. Swapping the template must disconnect the presented
		// control so the new template's presenter can adopt it.
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		static FuncControlTemplate CreateTemplate()
		{
			return new FuncControlTemplate<TestTemplatedControl>((_, _) => new Viewbox
			{
				Child = new ContentPresenter
				{
					[~ContentPresenter.ContentProperty] = new TemplateBinding(TestTemplatedControl.MyControlProperty)
				}
			});
		}

		var child = new Canvas();
		var target = new TestTemplatedControl
		{
			MyControl = child,
			Template = CreateTemplate()
		};

		var root = new TestRoot(target);
		root.ExecuteInitialLayoutPass();

		var oldPresenter = CornerstoneTest.IsType<ContentPresenter>(child.GetVisualParent());

		target.Template = CreateTemplate();
		target.ApplyTemplate();
		root.LayoutManager.ExecuteLayoutPass();

		var newPresenter = CornerstoneTest.IsType<ContentPresenter>(child.GetVisualParent());
		CornerstoneTest.NotSame(oldPresenter, newPresenter);
		CornerstoneTest.Same(root, child.GetVisualRoot());
	}

	[PresentationTestMethod]
	[DataRow(50, 100, 50, 100, 50, 100, 1)]
	[DataRow(50, 100, 150, 150, 50, 100, 1)]
	[DataRow(50, 100, 25, 50, 25, 50, 0.5)]
	public void ViewboxShouldReturnCorrectSizeAndScaleStretchDirectionDownOnly(
		double childWidth, double childHeight,
		double viewboxWidth, double viewboxHeight,
		double expectedWidth, double expectedHeight,
		double expectedScale)
	{
		var target = new Viewbox
		{
			Child = new Control { Width = childWidth, Height = childHeight },
			StretchDirection = StretchDirection.DownOnly
		};

		target.Measure(new Size(viewboxWidth, viewboxHeight));
		target.Arrange(new Rect(default, target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(expectedWidth, expectedHeight), target.DesiredSize);

		CornerstoneTest.IsTrue(TryGetScale(target, out var scale));
		CornerstoneTest.AreEqual(expectedScale, scale.X);
		CornerstoneTest.AreEqual(expectedScale, scale.Y);
	}

	[PresentationTestMethod]
	[DataRow(50, 100, 50, 100, 50, 100, 1)]
	[DataRow(50, 100, 25, 50, 25, 50, 1)]
	[DataRow(50, 100, 150, 150, 75, 150, 1.5)]
	public void ViewboxShouldReturnCorrectSizeAndScaleStretchDirectionUpOnly(
		double childWidth, double childHeight,
		double viewboxWidth, double viewboxHeight,
		double expectedWidth, double expectedHeight,
		double expectedScale)
	{
		var target = new Viewbox
		{
			Child = new Control { Width = childWidth, Height = childHeight },
			StretchDirection = StretchDirection.UpOnly
		};

		target.Measure(new Size(viewboxWidth, viewboxHeight));
		target.Arrange(new Rect(default, target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(expectedWidth, expectedHeight), target.DesiredSize);

		CornerstoneTest.IsTrue(TryGetScale(target, out var scale));
		CornerstoneTest.AreEqual(expectedScale, scale.X);
		CornerstoneTest.AreEqual(expectedScale, scale.Y);
	}

	[PresentationTestMethod]
	public void ViewboxStretchFillChild()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Viewbox { Stretch = Stretch.Fill, Child = new Rectangle { Width = 100, Height = 50 } };

		target.Measure(new Size(200, 200));
		target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(200, 200), target.DesiredSize);

		CornerstoneTest.IsTrue(TryGetScale(target, out var scale));
		CornerstoneTest.AreEqual(2.0, scale.X);
		CornerstoneTest.AreEqual(4.0, scale.Y);
	}

	[PresentationTestMethod]
	public void ViewboxStretchNoneChild()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Viewbox { Stretch = Stretch.None, Child = new Rectangle { Width = 100, Height = 50 } };

		target.Measure(new Size(200, 200));
		target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(100, 50), target.DesiredSize);

		CornerstoneTest.IsTrue(TryGetScale(target, out var scale));
		CornerstoneTest.AreEqual(1.0, scale.X);
		CornerstoneTest.AreEqual(1.0, scale.Y);
	}

	[PresentationTestMethod]
	public void ViewboxStretchUniformChild()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Viewbox { Child = new Rectangle { Width = 100, Height = 50 } };

		target.Measure(new Size(200, 200));
		target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(200, 100), target.DesiredSize);

		CornerstoneTest.IsTrue(TryGetScale(target, out var scale));
		CornerstoneTest.AreEqual(2.0, scale.X);
		CornerstoneTest.AreEqual(2.0, scale.Y);
	}

	[PresentationTestMethod]
	public void ViewboxStretchUniformChildWithUnrestrictedHeight()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Viewbox { Child = new Rectangle { Width = 100, Height = 50 } };

		target.Measure(new Size(200, double.PositiveInfinity));
		target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(200, 100), target.DesiredSize);

		CornerstoneTest.IsTrue(TryGetScale(target, out var scale));
		CornerstoneTest.AreEqual(2.0, scale.X);
		CornerstoneTest.AreEqual(2.0, scale.Y);
	}

	[PresentationTestMethod]
	public void ViewboxStretchUniformChildWithUnrestrictedWidth()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Viewbox { Child = new Rectangle { Width = 100, Height = 50 } };

		target.Measure(new Size(double.PositiveInfinity, 200));
		target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(400, 200), target.DesiredSize);

		CornerstoneTest.IsTrue(TryGetScale(target, out var scale));
		CornerstoneTest.AreEqual(4.0, scale.X);
		CornerstoneTest.AreEqual(4.0, scale.Y);
	}

	[PresentationTestMethod]
	public void ViewboxStretchUniformToFillChild()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new Viewbox { Stretch = Stretch.UniformToFill, Child = new Rectangle { Width = 100, Height = 50 } };

		target.Measure(new Size(200, 200));
		target.Arrange(new Rect(new Point(0, 0), target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(200, 200), target.DesiredSize);

		CornerstoneTest.IsTrue(TryGetScale(target, out var scale));
		CornerstoneTest.AreEqual(4.0, scale.X);
		CornerstoneTest.AreEqual(4.0, scale.Y);
	}

	private static bool TryGetScale(Viewbox viewbox, out Vector scale)
	{
		if (viewbox.InternalTransform is null)
		{
			scale = default;
			return false;
		}

		var matrix = viewbox.InternalTransform.Value;

		Matrix.TryDecomposeTransform(matrix, out var decomposed);

		scale = decomposed.Scale;
		return true;
	}

	#endregion

	#region Classes

	private class TestTemplatedControl : TemplatedControl
	{
		#region Fields

		public static readonly StyledProperty<Control> MyControlProperty =
			PresentationProperty.Register<TestTemplatedControl, Control>(nameof(MyControl));

		#endregion

		#region Properties

		public Control MyControl
		{
			get => GetValue(MyControlProperty);
			set => SetValue(MyControlProperty, value);
		}

		#endregion
	}

	#endregion
}