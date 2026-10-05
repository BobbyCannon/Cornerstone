#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class TemplateBindingTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CanBindIntPropertyToDouble()
	{
		var source = new Button
		{
			Opacity = 42,
			Template = new FuncControlTemplate<Button>((parent, _) =>
				new ContentPresenter
				{
					[~ContentPresenter.MaxLinesProperty] = new TemplateBinding(ContentControl.OpacityProperty)
				})
		};

		source.ApplyTemplate();

		var target = (ContentPresenter) source.GetVisualChildren().Single();

		CornerstoneTest.AreEqual(42, target.MaxLines);
	}

	[PresentationTestMethod]
	public void ConverterShouldBeUsed()
	{
		var source = new Button
		{
			Template = new FuncControlTemplate<Button>((parent, _) =>
				new ContentPresenter
				{
					[~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty)
					{
						Mode = BindingMode.TwoWay,
						Converter = new PrefixConverter(),
						ConverterParameter = "Hello "
					}
				})
		};

		source.ApplyTemplate();

		var target = (ContentPresenter) source.GetVisualChildren().Single();

		CornerstoneTest.IsNull(target.Content);
		source.Content = "foo";
		CornerstoneTest.AreEqual("Hello foo", target.Content);
		target.Content = "Hello bar";
		CornerstoneTest.AreEqual("bar", source.Content);
	}

	[PresentationTestMethod]
	public void OneWayBindingShouldBeSetUp()
	{
		var source = new Button
		{
			Template = new FuncControlTemplate<Button>((parent, _) =>
				new ContentPresenter
				{
					[~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty)
				})
		};

		source.ApplyTemplate();

		var target = (ContentPresenter) source.GetVisualChildren().Single();

		CornerstoneTest.IsNull(target.Content);
		source.Content = "foo";
		CornerstoneTest.AreEqual("foo", target.Content);
		source.Content = "bar";
		CornerstoneTest.AreEqual("bar", target.Content);
	}

	[PresentationTestMethod]
	public void ShouldExecuteConverterWithoutSpecificTargetType()
	{
		// See https://github.com/AvaloniaUI/Avalonia/issues/9766
		var source = new Button
		{
			Template = new FuncControlTemplate<Button>((parent, _) =>
				new ContentPresenter
				{
					[~ContentPresenter.IsVisibleProperty] = new MultiBinding
					{
						Converter = BoolConverters.And,
						Bindings =
						{
							new TemplateBinding(ContentControl.ContentProperty)
							{
								Converter = ObjectConverters.IsNotNull
							}
						}
					}
				})
		};

		source.ApplyTemplate();

		var target = (ContentPresenter) source.GetVisualChildren().Single();

		CornerstoneTest.IsFalse(target.IsVisible);
		source.Content = "foo";
		CornerstoneTest.IsTrue(target.IsVisible);
	}

	[PresentationTestMethod]
	public void ShouldNotPassUnsetValueToMultiBindingDuringApplyTemplate()
	{
		var converter = new MultiConverter();
		var source = new Button
		{
			Content = "foo",
			Template = new FuncControlTemplate<Button>((parent, _) =>
				new ContentPresenter
				{
					[~ContentPresenter.ContentProperty] = new MultiBinding
					{
						Converter = converter,
						Bindings =
						{
							new TemplateBinding(ContentControl.ContentProperty)
						}
					}
				})
		};

		source.ApplyTemplate();

		var target = (ContentPresenter) source.GetVisualChildren().Single();

		// #8672 was caused by TemplateBinding passing "unset" to the MultiBinding during
		// ApplyTemplate as the TemplatedParent property doesn't get setup until after the
		// binding is initiated.
		CornerstoneTest.AreEqual(new[] { "foo" }, converter.Values);
	}

	[PresentationTestMethod]
	public void ShouldWorkInsideOfFlyout()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var source = new Button
			{
				Template = new FuncControlTemplate<Button>((parent, _) =>
					new Button
					{
						Flyout = new Flyout
						{
							Content = new TextBlock
							{
								[~TextBlock.TextProperty] = new TemplateBinding(ContentControl.ContentProperty)
							}
						}
					})
			};

			window.Content = source;
			window.Show();
			try
			{
				var templateChild = (Button) source.GetVisualChildren().Single();
				templateChild.Flyout!.ShowAt(templateChild);

				var target = (TextBlock) ((Flyout) templateChild.Flyout).Content!;

				target[~TextBlock.TextProperty] = new TemplateBinding(ContentControl.ContentProperty);
				CornerstoneTest.IsNull(target.Text);
				source.Content = "foo";
				CornerstoneTest.AreEqual("foo", target.Text);
				source.Content = "bar";
				CornerstoneTest.AreEqual("bar", target.Text);
			}
			finally
			{
				window.Close();
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldWorkInsideOfPopup()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var source = new Button
			{
				Template = new FuncControlTemplate<Button>((parent, _) =>
					new Popup
					{
						Child = new TextBlock
						{
							[~TextBlock.TextProperty] = new TemplateBinding(ContentControl.ContentProperty)
						}
					})
			};

			window.Content = source;
			window.Show();
			try
			{
				var popup = (Popup) source.GetVisualChildren().Single();
				popup.IsOpen = true;

				var target = (TextBlock) popup.Child!;

				target[~TextBlock.TextProperty] = new TemplateBinding(ContentControl.ContentProperty);
				CornerstoneTest.IsNull(target.Text);
				source.Content = "foo";
				CornerstoneTest.AreEqual("foo", target.Text);
				source.Content = "bar";
				CornerstoneTest.AreEqual("bar", target.Text);
			}
			finally
			{
				window.Close();
			}
		}
	}

	[PresentationTestMethod]
	public void ShouldWorkInsideOfTooltip()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window();
			var source = new Button
			{
				Template = new FuncControlTemplate<Button>((parent, _) =>
					new Decorator
					{
						[ToolTip.TipProperty] = new TextBlock
						{
							[~TextBlock.TextProperty] = new TemplateBinding(ContentControl.ContentProperty)
						}
					})
			};

			window.Content = source;
			window.Show();
			try
			{
				var templateChild = (Decorator) source.GetVisualChildren().Single();
				ToolTip.SetIsOpen(templateChild, true);

				var target = (TextBlock) ToolTip.GetTip(templateChild)!;

				CornerstoneTest.IsNull(target.Text);
				source.Content = "foo";
				CornerstoneTest.AreEqual("foo", target.Text);
				source.Content = "bar";
				CornerstoneTest.AreEqual("bar", target.Text);
			}
			finally
			{
				window.Close();
			}
		}
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldBeSetUp()
	{
		var source = new Button
		{
			Template = new FuncControlTemplate<Button>((parent, _) =>
				new ContentPresenter
				{
					[~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty)
					{
						Mode = BindingMode.TwoWay
					}
				})
		};

		source.ApplyTemplate();

		var target = (ContentPresenter) source.GetVisualChildren().Single();

		CornerstoneTest.IsNull(target.Content);
		source.Content = "foo";
		CornerstoneTest.AreEqual("foo", target.Content);
		target.Content = "bar";
		CornerstoneTest.AreEqual("bar", source.Content);
	}

	#endregion

	#region Classes

	private class MultiConverter : IMultiValueConverter
	{
		#region Properties

		public List<object> Values { get; } = new();

		#endregion

		#region Methods

		public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
		{
			Values.AddRange(values);
			return values.FirstOrDefault();
		}

		#endregion
	}

	private class PrefixConverter : IValueConverter
	{
		#region Methods

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((value != null) && (parameter != null))
			{
				return parameter.ToString() + value;
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((value != null) && (parameter != null))
			{
				var s = value.ToString() ?? string.Empty;
				var prefix = parameter.ToString() ?? string.Empty;

				if (s.StartsWith(prefix))
				{
					return s.Substring(prefix.Length);
				}

				return s;
			}

			return null;
		}

		#endregion
	}

	#endregion
}