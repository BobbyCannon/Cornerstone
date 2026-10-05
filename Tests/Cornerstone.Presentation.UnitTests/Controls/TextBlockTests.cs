#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TextBlockTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CallingArrangeWithDifferentSizeShouldUpdateConstraintAndTextLayout()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var textBlock = new TestTextBlock { Text = "Hello World" };

			textBlock.Measure(Size.Infinity);

			var textLayout = textBlock.TextLayout;

			var constraint = LayoutHelper.RoundLayoutSizeUp(new Size(textLayout.WidthIncludingTrailingWhitespace, textLayout.Height), 1);

			textBlock.Arrange(new Rect(constraint));

			//TextLayout is recreated after arrange
			textLayout = textBlock.TextLayout;

			CornerstoneTest.AreEqual(constraint, textBlock.Constraint);

			textBlock.Measure(constraint);

			CornerstoneTest.AreEqual(textLayout, textBlock.TextLayout);

			constraint += new Size(50, 0);

			textBlock.Arrange(new Rect(constraint));

			CornerstoneTest.AreEqual(constraint, textBlock.Constraint);

			//TextLayout is recreated after arrange
			CornerstoneTest.AreNotEqual(textLayout, textBlock.TextLayout);
		}
	}

	[PresentationTestMethod]
	public void CallingMeasureShouldUpdateTextLayout()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var textBlock = new TestTextBlock { Text = "Hello World" };

			var constraint = textBlock.Constraint;
			CornerstoneTest.IsTrue(double.IsNaN(constraint.Width));
			CornerstoneTest.IsTrue(double.IsNaN(constraint.Height));

			textBlock.Measure(new Size(100, 100));

			var textLayout = textBlock.TextLayout;

			textBlock.Measure(new Size(50, 100));

			CornerstoneTest.AreNotEqual(textLayout, textBlock.TextLayout);
		}
	}

	[PresentationTestMethod]
	public void CallingMeasureWithInfiniteSpaceShouldSetDesiredSize()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var textBlock = new TestTextBlock { Text = "Hello World" };

			textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

			var textLayout = textBlock.TextLayout;

			var constraint = LayoutHelper.RoundLayoutSizeUp(new Size(textLayout.WidthIncludingTrailingWhitespace, textLayout.Height), 1);

			CornerstoneTest.AreEqual(constraint, textBlock.DesiredSize);
		}
	}

	[PresentationTestMethod]
	public void CanCallMeasureWithoutInvalidateTextLayout()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextBlock();

			target.Inlines!.Add(new TextBox { Text = "Hello" });

			target.Measure(Size.Infinity);

			target.InvalidateMeasure();

			target.Measure(Size.Infinity);
		}
	}

	[PresentationTestMethod]
	public void ChangingInlineHostShouldPropagateToNestedInlines()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextBlock();

			var span = new Span { Inlines = new InlineCollection { new Run { Text = "World" } } };

			var inlines = new InlineCollection { new Run { Text = "Hello " }, span };

			target.Inlines = inlines;

			CornerstoneTest.AreEqual(target, span.InlineHost);
		}
	}

	[PresentationTestMethod]
	public void ChangingInlinesCollectionShouldInvalidateMeasure()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextBlock();

			target.Measure(Size.Infinity);

			CornerstoneTest.IsTrue(target.IsMeasureValid);

			target.Inlines!.Add(new Run("Hello"));

			CornerstoneTest.IsFalse(target.IsMeasureValid);

			target.Measure(Size.Infinity);

			CornerstoneTest.IsTrue(target.IsMeasureValid);
		}
	}

	[PresentationTestMethod]
	public void ChangingInlinesPropertiesShouldInvalidateMeasure()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextBlock();

			var inline = new Run("Hello");

			target.Inlines!.Add(inline);

			target.Measure(Size.Infinity);

			CornerstoneTest.IsTrue(target.IsMeasureValid);

			inline.Foreground = Brushes.Green;

			CornerstoneTest.IsFalse(target.IsMeasureValid);
		}
	}

	[PresentationTestMethod]
	public void ChangingInlinesShouldAttachEmbeddedControlsToParents()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextBlock();

			var control = new Border();

			var inlineUIContainer = new InlineUIContainer { Child = control };

			target.Inlines = new InlineCollection { inlineUIContainer };

			CornerstoneTest.AreEqual(inlineUIContainer, control.Parent);

			CornerstoneTest.AreEqual(target, control.VisualParent);
		}
	}

	[PresentationTestMethod]
	public void ChangingInlinesShouldInvalidateMeasure()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextBlock();

			var inlines = new InlineCollection { new Run("Hello") };

			target.Measure(Size.Infinity);

			CornerstoneTest.IsTrue(target.IsMeasureValid);

			target.Inlines = inlines;

			CornerstoneTest.IsFalse(target.IsMeasureValid);
		}
	}

	[PresentationTestMethod]
	public void ChangingInlinesShouldResetInlineUIContainerVisualParentOnMeasure()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextBlock();

			var control = new Control();

			var run = new InlineUIContainer(control);

			target.Inlines!.Add(run);

			target.Measure(Size.Infinity);

			CornerstoneTest.IsTrue(target.IsMeasureValid);

			CornerstoneTest.AreEqual(target, control.VisualParent);

			target.Inlines = null;

			CornerstoneTest.IsNull(run.Parent);

			target.Inlines = new InlineCollection { new Run("Hello World") };

			CornerstoneTest.IsNull(run.Parent);

			target.Measure(Size.Infinity);

			CornerstoneTest.IsNull(control.VisualParent);
		}
	}

	[PresentationTestMethod]
	public void ChangingInlinesShouldResetInlinesParent()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextBlock();

			var run = new Run("Hello");

			target.Inlines!.Add(run);

			target.Measure(Size.Infinity);

			CornerstoneTest.IsTrue(target.IsMeasureValid);

			target.Inlines = null;

			CornerstoneTest.IsNull(run.Parent);

			target.Inlines = new InlineCollection { run };

			CornerstoneTest.AreEqual(target, run.Parent);
		}
	}

	[PresentationTestMethod]
	public void ChangingInlinesShouldResetVisualChildren()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new TextBlock();

			target.Inlines!.Add(new Border());

			target.Measure(Size.Infinity);

			CornerstoneTest.NotEmpty(target.VisualChildren);

			target.Inlines = null;

			CornerstoneTest.Empty(target.VisualChildren);
		}
	}

	[PresentationTestMethod]
	public void DefaultBindingModeShouldBeOneWay()
	{
		CornerstoneTest.AreEqual(BindingMode.OneWay, TextBlock.TextProperty.GetMetadata(typeof(TextBlock)).DefaultBindingMode);
	}

	[PresentationTestMethod]
	public void DefaultTextValueShouldBeNull()
	{
		var textBlock = new TextBlock();

		CornerstoneTest.AreEqual(null, textBlock.Text);
	}

	[PresentationTestMethod]
	public void EmbeddedControlShouldKeepFocus()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target = new TextBlock();

			var root = new TestRoot
			{
				Child = target
			};

			var textBox = new TextBox { Text = "Hello", Template = TextBoxTests.CreateTemplate() };

			target.Inlines!.Add(textBox);

			target.Measure(Size.Infinity);

			textBox.Focus();

			CornerstoneTest.Same(textBox, root.FocusManager.GetFocusedElement());

			target.InvalidateMeasure();

			CornerstoneTest.Same(textBox, root.FocusManager.GetFocusedElement());

			target.Measure(Size.Infinity);

			CornerstoneTest.Same(textBox, root.FocusManager.GetFocusedElement());
		}
	}

	[PresentationTestMethod]
	public void InlineUIContainerChildShouldBeArranged()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TextBlock();

			var button = new Button { Content = "12345678" };

			button.Template = new FuncControlTemplate<Button>((parent, scope) =>
				new TextBlock
				{
					Name = "PART_ContentPresenter",
					[!TextBlock.TextProperty] = parent[!ContentControl.ContentProperty]
				}.RegisterInNameScope(scope)
			);

			target.Inlines!.Add("123456");
			target.Inlines.Add(new InlineUIContainer(button));
			target.Inlines.Add("123456");

			target.Measure(Size.Infinity);
			target.Arrange(new Rect(target.DesiredSize));

			CornerstoneTest.IsTrue(button.IsMeasureValid);
			CornerstoneTest.AreEqual(58, button.DesiredSize.Width);

			target.Arrange(new Rect(new Size(200, 50)));

			CornerstoneTest.IsTrue(button.IsArrangeValid);

			CornerstoneTest.AreEqual(43, button.Bounds.Left);
		}
	}

	[PresentationTestMethod]
	public void InlineUIContainerChildShouldBeConstrained()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TextBlock();

			var drawing = new GeometryDrawing();
			drawing.Geometry = new RectangleGeometry(new Rect(0, 0, 500, 500));
			var image = new DrawingImage(drawing);

			var imageControl = new Image { Source = image };
			var container = new InlineUIContainer(imageControl);

			target.Inlines!.Add(new Run("The child should not be limited by position on line."));
			target.Inlines.Add(container);

			target.Measure(new Size(100, 100));
			target.Arrange(new Rect(target.DesiredSize));

			CornerstoneTest.IsTrue(imageControl.IsMeasureValid);
			CornerstoneTest.AreEqual(100, imageControl.Bounds.Width);
		}
	}

	[PresentationTestMethod]
	public void LetterSpacingPropertyUsesTextElementDefinition()
	{
		CornerstoneTest.Same(TextElement.LetterSpacingProperty, TextBlock.LetterSpacingProperty);
	}

	[PresentationTestMethod]
	public void MeasureAndArrangeShouldUseWidthIncludingTrailingWhitespaceForBounds()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new TextBlock
		{
			Text = "fy",
			FontStyle = FontStyle.Italic,
			FontSize = 48,
			UseLayoutRounding = false,
			Padding = new Thickness(3, 2, 5, 4)
		};

		target.Measure(Size.Infinity);

		var expectedSize =
			new Size(target.TextLayout.WidthIncludingTrailingWhitespace, target.TextLayout.Height)
				.Inflate(target.Padding);

		CornerstoneTest.AreEqual(expectedSize, target.DesiredSize);

		target.Arrange(new Rect(default, target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(default, expectedSize), target.Bounds);
	}

	[PresentationTestMethod]
	public void SettingTextDecorationsShouldUpdateInlines()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TextBlock();

			target.Inlines!.Add(new Run("Hello World"));

			CornerstoneTest.AreEqual(1, target.Inlines.Count);

			CornerstoneTest.IsNull(target.Inlines[0].TextDecorations);

			var underline = TextDecorations.Underline;

			target.TextDecorations = underline;

			CornerstoneTest.AreEqual(underline, target.Inlines[0].TextDecorations);
		}
	}

	[PresentationTestMethod]
	public void SettingTextShouldResetInlines()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TextBlock();

			target.Inlines!.Add(new Run("Hello World"));

			CornerstoneTest.AreEqual(null, target.Text);

			CornerstoneTest.AreEqual(1, target.Inlines.Count);

			target.Text = "1234";

			CornerstoneTest.AreEqual("1234", target.Text);

			CornerstoneTest.AreEqual(0, target.Inlines.Count);
		}
	}

	[PresentationTestMethod]
	public void ShouldMeasureMinTextWith()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var textBlock = new TextBlock
			{
				Text = "Hello&#10;שלום&#10;Really really really really long line",
				HorizontalAlignment = HorizontalAlignment.Center,
				TextAlignment = TextAlignment.DetectFromContent,
				TextWrapping = TextWrapping.Wrap
			};

			textBlock.Measure(new Size(1920, 1080));

			var textLayout = textBlock.TextLayout;

			var constraint = LayoutHelper.RoundLayoutSizeUp(new Size(textLayout.Width, textLayout.Height), 1);

			CornerstoneTest.AreEqual(constraint, textBlock.DesiredSize);
		}
	}

	[PresentationTestMethod]
	public void TextBlockTextLinesShouldBeEmpty()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var textblock = new TextBlock();
			textblock.Inlines?.Add(new Run("123"));
			textblock.Measure(new Size(200, 200));
			var count = textblock.TextLayout.TextLines[0].TextRuns.Count;
			textblock.Inlines?.Clear();
			textblock.Measure(new Size(200, 200));
			var count1 = textblock.TextLayout.TextLines[0].TextRuns.Count;
			CornerstoneTest.AreNotEqual(count, count1);
		}
	}

	[PresentationTestMethod]
	public void TextBlockWithFractionalLineHeightShouldNotCullLastLineAtFractionalScaling()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new TextBlock
		{
			Text = "first second third",
			FontSize = 16,
			LineHeight = 20.8,
			TextWrapping = TextWrapping.Wrap,
			Width = 50,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		var root = new TestRoot(target)
		{
			LayoutScaling = 1.25
		};

		root.Measure(Size.Infinity);
		root.Arrange(new Rect(root.DesiredSize));

		CornerstoneTest.AreEqual(3, target.TextLayout.TextLines.Count);
	}

	[PresentationTestMethod]
	public void TextBlockWithInfiniteSizeShouldBeRemeasuredAfterTextLayoutCreated()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new TextBlock { Text = "" };
		var layout = target.TextLayout;

		CornerstoneTest.AreEqual(0.0, layout.MaxWidth);
		CornerstoneTest.AreEqual(0.0, layout.MaxHeight);

		target.Text = "foo";
		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

		CornerstoneTest.IsTrue(target.DesiredSize.Width > 0);
		CornerstoneTest.IsTrue(target.DesiredSize.Height > 0);
	}

	[PresentationTestMethod]
	public void TextBlockWithUseLayoutRoundingFalseShouldNotRoundBounds()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new TextBlock { Text = "1980", UseLayoutRounding = false };

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		target.Arrange(new Rect(default, target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 27.954545454545453, 14.522727272727273), target.Bounds);
	}

	[PresentationTestMethod]
	public void TextBlockWithUseLayoutRoundingFalseShouldNotRoundDesiredSize()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new TextBlock { Text = "1980", UseLayoutRounding = false };

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

		CornerstoneTest.AreEqual(new Size(27.954545454545453, 14.522727272727273), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void TextBlockWithUseLayoutRoundingFalseShouldNotRoundPaddingInArrangeOverride()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new TextBlock { Text = "1980", UseLayoutRounding = false, Padding = new(2.25) };

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		target.Arrange(new Rect(default, target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 32.45454545454545, 19.022727272727273), target.Bounds);
	}

	[PresentationTestMethod]
	public void TextBlockWithUseLayoutRoundingFalseShouldNotRoundPaddingInMeasureOverride()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new TextBlock { Text = "1980", UseLayoutRounding = false, Padding = new(2.25) };

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

		CornerstoneTest.AreEqual(new Size(32.45454545454545, 19.022727272727273), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void TextBlockWithUseLayoutRoundingTrueShouldRoundDesiredSize()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new TextBlock { Text = "1980" };

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

		CornerstoneTest.AreEqual(new Size(28, 15), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void TextBlockWithUseLayoutRoundingTrueShouldRoundPaddingAndDesiredSize()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var target = new TextBlock { Text = "1980", Padding = new(2.25) };

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

		CornerstoneTest.AreEqual(new Size(32, 19), target.DesiredSize);
	}

	#endregion

	#region Classes

	private class TestTextBlock : TextBlock
	{
		#region Properties

		public Size Constraint => _constraint;

		#endregion
	}

	#endregion
}