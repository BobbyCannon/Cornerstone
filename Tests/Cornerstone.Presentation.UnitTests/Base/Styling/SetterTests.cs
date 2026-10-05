#region References

using System;
using System.Globalization;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SetterTests
{
	#region Methods

	[PresentationTestMethod]
	public void CanSetDirectPropertyBindingInStyleWithoutActivator()
	{
		var control = new DirectPropertyClass();
		var target = new Setter();
		var source = new BehaviorSubject<object>("foo");
		var style = new Style(x => x.Is<DirectPropertyClass>())
		{
			Setters =
			{
				new Setter(DirectPropertyClass.FooProperty, source.ToBinding())
			}
		};

		Apply(style, control);

		CornerstoneTest.AreEqual("foo", control.Foo);
	}

	[PresentationTestMethod]
	public void CanSetDirectPropertyInStyleWithoutActivator()
	{
		var control = new DirectPropertyClass();
		var target = new Setter();
		var style = new Style(x => x.Is<DirectPropertyClass>())
		{
			Setters =
			{
				new Setter(DirectPropertyClass.FooProperty, "foo")
			}
		};

		Apply(style, control);

		CornerstoneTest.AreEqual("foo", control.Foo);
	}

	[PresentationTestMethod]
	public void CannotAssignControlToValue()
	{
		var target = new Setter();

		Assert.Throws<InvalidOperationException>(() => target.Value = new Border());
	}

	[PresentationTestMethod]
	public void CannotSetDirectPropertyBindingInStyleWithActivator()
	{
		var control = new DirectPropertyClass();
		var target = new Setter();
		var source = new BehaviorSubject<object>("foo");
		var style = new Style(x => x.Is<DirectPropertyClass>().Class("foo"))
		{
			Setters =
			{
				new Setter(DirectPropertyClass.FooProperty, source.ToBinding())
			}
		};

		Assert.Throws<InvalidOperationException>(() => Apply(style, control));
	}

	[PresentationTestMethod]
	public void CannotSetDirectPropertyInStyleWithActivator()
	{
		var control = new DirectPropertyClass();
		var target = new Setter();
		var style = new Style(x => x.Is<DirectPropertyClass>().Class("foo"))
		{
			Setters =
			{
				new Setter(DirectPropertyClass.FooProperty, "foo")
			}
		};

		Assert.Throws<InvalidOperationException>(() => Apply(style, control));
	}

	[PresentationTestMethod]
	public void DirectPropertySetterWithTwoWayBindingShouldUpdateSource()
	{
		using var app = UnitTestApplication.Start(TestServices.MockThreadingInterface);
		var data = new Data { Foo = "foo" };
		var control = new DirectPropertyClass
		{
			DataContext = data
		};

		var style = new Style(x => x.OfType<DirectPropertyClass>())
		{
			Setters =
			{
				new Setter
				{
					Property = DirectPropertyClass.FooProperty,
					Value = new Binding
					{
						Path = "Foo",
						Mode = BindingMode.TwoWay
					}
				}
			}
		};

		Apply(style, control);
		CornerstoneTest.AreEqual("foo", control.Foo);

		control.Foo = "bar";
		CornerstoneTest.AreEqual("bar", data.Foo);
	}

	[PresentationTestMethod]
	public void DoesNotCallConverterConvertBackOnOneWayBinding()
	{
		var control = new Decorator
		{
			Name = "foo",
			Classes = { "foo" }
		};

		var binding = new Binding("Name")
		{
			Mode = BindingMode.OneWay,
			Converter = new TestConverter(),
			RelativeSource = new RelativeSource(RelativeSourceMode.Self)
		};

		var style = new Style(x => x.OfType<Decorator>().Class("foo"))
		{
			Setters =
			{
				new Setter(Decorator.TagProperty, binding)
			}
		};

		Apply(style, control);

		CornerstoneTest.AreEqual("foobar", control.Tag);

		// Issue #1218 caused TestConverter.ConvertBack to throw here.
		control.Classes.Remove("foo");
		CornerstoneTest.IsNull(control.Tag);
	}

	[PresentationTestMethod]
	public void NonActiveStyledPropertyBindingShouldBeUnsubscribed()
	{
		var data = new Data { Bar = Brushes.Red };
		var control = new Border
		{
			DataContext = data
		};

		var style1 = new Style(x => x.OfType<Border>())
		{
			Setters =
			{
				new Setter
				{
					Property = Border.BackgroundProperty,
					Value = new Binding("Bar")
				}
			}
		};

		var style2 = new Style(x => x.OfType<Border>().Class("foo"))
		{
			Setters =
			{
				new Setter
				{
					Property = Border.BackgroundProperty,
					Value = Brushes.Green
				}
			}
		};

		Apply(style1, control);
		Apply(style2, control);

		// `style1` is initially active.
		CornerstoneTest.AreEqual(Brushes.Red, control.Background);
		CornerstoneTest.AreEqual(1, data.PropertyChangedSubscriptionCount);

		// Activate `style2`.
		control.Classes.Add("foo");
		CornerstoneTest.AreEqual(Brushes.Green, control.Background);

		// The binding from `style1` is now inactive and so should be unsubscribed.
		CornerstoneTest.AreEqual(0, data.PropertyChangedSubscriptionCount);
	}

	[PresentationTestMethod]
	public void NonActiveStyledPropertySetterWithTwoWayBindingShouldNotUpdateSource()
	{
		var data = new Data { Bar = Brushes.Red };
		var control = new Border
		{
			DataContext = data
		};

		var style1 = new Style(x => x.OfType<Border>())
		{
			Setters =
			{
				new Setter
				{
					Property = Border.BackgroundProperty,
					Value = new Binding
					{
						Path = "Bar",
						Mode = BindingMode.TwoWay
					}
				}
			}
		};

		var style2 = new Style(x => x.OfType<Border>().Class("foo"))
		{
			Setters =
			{
				new Setter
				{
					Property = Border.BackgroundProperty,
					Value = Brushes.Green
				}
			}
		};

		Apply(style1, control);
		Apply(style2, control);

		// `style1` is initially active.
		CornerstoneTest.AreEqual(Brushes.Red, control.Background);

		// Activate `style2`.
		control.Classes.Add("foo");
		CornerstoneTest.AreEqual(Brushes.Green, control.Background);

		// The two-way binding from `style1` is now inactive and so should not write back to
		// the DataContext.
		CornerstoneTest.AreEqual(Brushes.Red, data.Bar);
	}

	[PresentationTestMethod]
	public void SetterShouldApplyBindingToProperty()
	{
		var control = new TextBlock();
		var subject = new BehaviorSubject<object>("foo");
		var binding = subject.ToBinding();
		var setter = new Setter(TextBlock.TagProperty, binding);

		Apply(setter, control);

		CornerstoneTest.AreEqual("foo", control.Tag);
	}

	[PresentationTestMethod]
	public void SetterShouldApplyBindingWithActivatorWithStyleTriggerPriority()
	{
		var control = new Border
		{
			Classes = { "foo" },
			DataContext = "foo"
		};

		var style = new Style(x => x.OfType<Border>().Class("foo"))
		{
			Setters =
			{
				new Setter(Control.TagProperty, new Binding())
			}
		};

		var raised = 0;

		control.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(Control.TagProperty, e.Property);
			CornerstoneTest.AreEqual(BindingPriority.StyleTrigger, e.Priority);
			++raised;
		};

		Apply(style, control);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void SetterShouldApplyBindingWithoutActivatorWithStylePriority()
	{
		var control = new Border
		{
			DataContext = "foo"
		};

		var style = new Style(x => x.OfType<Border>())
		{
			Setters =
			{
				new Setter(Control.TagProperty, new Binding())
			}
		};

		var raised = 0;

		control.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(Control.TagProperty, e.Property);
			CornerstoneTest.AreEqual(BindingPriority.Style, e.Priority);
			++raised;
		};

		Apply(style, control);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void SetterShouldApplyValueWithActivatorWithStyleTriggerPriority()
	{
		var control = new Border { Classes = { "foo" } };
		var style = new Style(x => x.OfType<Border>().Class("foo"))
		{
			Setters =
			{
				new Setter(Control.TagProperty, "foo")
			}
		};
		var activator = new Subject<bool>();
		var raised = 0;

		control.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(Border.TagProperty, e.Property);
			CornerstoneTest.AreEqual(BindingPriority.StyleTrigger, e.Priority);
			++raised;
		};

		Apply(style, control);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void SetterShouldApplyValueWithoutActivatorWithStylePriority()
	{
		var control = new Border();
		var style = new Style(x => x.OfType<Border>())
		{
			Setters =
			{
				new Setter(Control.TagProperty, "foo")
			}
		};
		var raised = 0;

		control.PropertyChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(Control.TagProperty, e.Property);
			CornerstoneTest.AreEqual(BindingPriority.Style, e.Priority);
			++raised;
		};

		Apply(style, control);

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void SetterShouldHandleBindingProducingUnsetValue()
	{
		var control = new TextBlock();
		var subject = new BehaviorSubject<object>(PresentationProperty.UnsetValue);
		var binding = subject.ToBinding();
		var setter = new Setter(TextBlock.TagProperty, binding);

		Apply(setter, control);

		CornerstoneTest.AreEqual(null, control.Text);
	}

	[PresentationTestMethod]
	public void SetterShouldMaterializeTemplateToProperty()
	{
		var control = new Decorator();
		var template = new FuncTemplate<Canvas>(() => new Canvas());
		var style = new StubStyle();
		var setter = new Setter(Decorator.ChildProperty, template);

		Apply(setter, control);

		CornerstoneTest.IsType<Canvas>(control.Child);
	}

	[PresentationTestMethod]
	public void StyledPropertySetterWithTwoWayBindingShouldUpdateSource()
	{
		var data = new Data { Bar = Brushes.Red };
		var control = new Border
		{
			DataContext = data
		};

		var style = new Style(x => x.OfType<Border>())
		{
			Setters =
			{
				new Setter
				{
					Property = Border.BackgroundProperty,
					Value = new Binding
					{
						Path = "Bar",
						Mode = BindingMode.TwoWay
					}
				}
			}
		};

		Apply(style, control);
		CornerstoneTest.AreEqual(Brushes.Red, control.Background);

		control.Background = Brushes.Green;
		CornerstoneTest.AreEqual(Brushes.Green, data.Bar);
	}

	[PresentationTestMethod]
	public void StyledPropertySetterWithTwoWayBindingUpdatesSourceWhenMadeActive()
	{
		var data = new Data { Bar = Brushes.Red };
		var control = new Border
		{
			Classes = { "foo" },
			DataContext = data
		};

		var style1 = new Style(x => x.OfType<Border>())
		{
			Setters =
			{
				new Setter
				{
					Property = Border.BackgroundProperty,
					Value = new Binding
					{
						Path = "Bar",
						Mode = BindingMode.TwoWay
					}
				}
			}
		};

		var style2 = new Style(x => x.OfType<Border>().Class("foo"))
		{
			Setters =
			{
				new Setter
				{
					Property = Border.BackgroundProperty,
					Value = Brushes.Green
				}
			}
		};

		Apply(style1, control);
		Apply(style2, control);

		// `style2` is initially active.
		CornerstoneTest.AreEqual(Brushes.Green, control.Background);

		// Deactivate `style2`.
		control.Classes.Remove("foo");
		CornerstoneTest.AreEqual(Brushes.Red, control.Background);

		// The two-way binding from `style1` is now active and so should write back to the
		// DataContext.
		control.Background = Brushes.Blue;
		CornerstoneTest.AreEqual(Brushes.Blue, data.Bar);
	}

	private void Apply(Style style, StyledElement element)
	{
		StyleHelpers.TryAttach(style, element);
	}

	private void Apply(Setter setter, Control control)
	{
		var style = new Style(x => x.Is<Control>())
		{
			Setters = { setter }
		};

		Apply(style, control);
	}

	#endregion

	#region Classes

	private class Data : NotifyingBase
	{
		#region Properties

		public IBrush Bar { get; set; }
		public string Foo { get; set; }

		#endregion
	}

	private class DirectPropertyClass : StyledElement
	{
		#region Fields

		public static readonly DirectProperty<DirectPropertyClass, string> FooProperty = PresentationProperty.RegisterDirect<DirectPropertyClass, string>(nameof(Foo),
			x => x.Foo, (x, v) => x.Foo = v);

		private string _foo;

		#endregion

		#region Properties

		public string Foo
		{
			get => _foo;
			set => SetAndRaise(FooProperty, ref _foo, value);
		}

		#endregion
	}

	private class TestConverter : IValueConverter
	{
		#region Methods

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value + "bar";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	#endregion
}