#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Base.Animation;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class StyleTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AddingNestedStyleShouldAttachToControl()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Styles =
			{
				new Styles
				{
					new Style(x => x.OfType<Border>())
					{
						Setters =
						{
							new Setter(Border.BorderThicknessProperty, new Thickness(4))
						}
					}
				}
			},
			Child = border
		};

		root.Measure(Size.Infinity);
		CornerstoneTest.AreEqual(new Thickness(4), border.BorderThickness);

		((Styles) root.Styles[0]).Add(new Style(x => x.OfType<Border>())
		{
			Setters =
			{
				new Setter(Border.BorderThicknessProperty, new Thickness(6))
			}
		});

		root.Measure(Size.Infinity);
		CornerstoneTest.AreEqual(new Thickness(6), border.BorderThickness);
	}

	[PresentationTestMethod]
	public void AddingStyleShouldAttachToControl()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<Border>())
				{
					Setters =
					{
						new Setter(Border.BorderThicknessProperty, new Thickness(4))
					}
				}
			},
			Child = border
		};

		root.Measure(Size.Infinity);
		CornerstoneTest.AreEqual(new Thickness(4), border.BorderThickness);

		root.Styles.Add(new Style(x => x.OfType<Border>())
		{
			Setters =
			{
				new Setter(Border.BorderThicknessProperty, new Thickness(6))
			}
		});

		root.Measure(Size.Infinity);
		CornerstoneTest.AreEqual(new Thickness(6), border.BorderThickness);
	}

	[PresentationTestMethod]
	public void AddingStyleWithNoSettersOrAnimationsShouldNotInvalidateStyles()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<Border>())
				{
					Setters =
					{
						new Setter(Border.BorderThicknessProperty, new Thickness(4))
					}
				}
			},
			Child = border
		};

		root.Measure(Size.Infinity);
		CornerstoneTest.AreEqual(new Thickness(4), border.BorderThickness);

		root.Styles.Add(new Style(x => x.OfType<Border>()));

		CornerstoneTest.AreEqual(new Thickness(4), border.BorderThickness);
	}

	[PresentationTestMethod]
	public void AnimationsShouldBeActivated()
	{
		var style = new Style(x => x.OfType<Class1>())
		{
			Animations =
			{
				new Presentation.Animation.Animation
				{
					Duration = TimeSpan.FromSeconds(1),
					Children =
					{
						new KeyFrame
						{
							Setters =
							{
								new Setter { Property = Class1.DoubleProperty, Value = 5.0 }
							}
						},
						new KeyFrame
						{
							Setters =
							{
								new Setter { Property = Class1.DoubleProperty, Value = 10.0 }
							},
							Cue = new Cue(1d)
						}
					}
				}
			}
		};

		var clock = new TestClock();
		var target = new Class1 { Clock = clock };

		StyleHelpers.TryAttach(style, target);

		CornerstoneTest.AreEqual(0.0, target.Double);

		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(5.0, target.Double);

		clock.Step(TimeSpan.FromSeconds(0.5));
		CornerstoneTest.AreEqual(7.5, target.Double);
	}

	[PresentationTestMethod]
	public void AnimationsWithActivatorTriggerShouldBeActivatedAndDeactivated()
	{
		var clock = new TestClock();
		var border = new Border();

		var root = new TestRoot
		{
			Clock = clock,
			Styles =
			{
				new Style(x => x.OfType<Border>().Not(default(Selector).Class("foo")))
				{
					Setters =
					{
						new Setter(Border.BackgroundProperty, Brushes.Yellow)
					},
					Animations =
					{
						new Presentation.Animation.Animation
						{
							Duration = TimeSpan.FromSeconds(1.0),
							Children =
							{
								new KeyFrame
								{
									Setters =
									{
										new Setter(Border.BackgroundProperty, Brushes.Green)
									},
									Cue = new Cue(0.0)
								},
								new KeyFrame
								{
									Setters =
									{
										new Setter(Border.BackgroundProperty, Brushes.Green)
									},
									Cue = new Cue(1.0)
								}
							}
						}
					}
				},
				new Style(x => x.OfType<Border>().Class("foo"))
				{
					Setters =
					{
						new Setter(Border.BackgroundProperty, Brushes.Blue)
					}
				}
			},
			Child = border
		};

		root.Measure(Size.Infinity);

		CornerstoneTest.AreEqual(Brushes.Yellow, border.Background);

		clock.Step(TimeSpan.FromSeconds(0.5));
		CornerstoneTest.AreEqual(Brushes.Green, border.Background);

		border.Classes.Add("foo");
		CornerstoneTest.AreEqual(Brushes.Blue, border.Background);
	}

	[PresentationTestMethod]
	public void AnimationsWithTriggerShouldBeActivatedAndDeactivated()
	{
		var style = new Style(x => x.OfType<Class1>().Class("foo"))
		{
			Animations =
			{
				new Presentation.Animation.Animation
				{
					Duration = TimeSpan.FromSeconds(1),
					Children =
					{
						new KeyFrame
						{
							Setters =
							{
								new Setter { Property = Class1.DoubleProperty, Value = 5.0 }
							}
						},
						new KeyFrame
						{
							Setters =
							{
								new Setter { Property = Class1.DoubleProperty, Value = 10.0 }
							},
							Cue = new Cue(1d)
						}
					}
				}
			}
		};

		var clock = new TestClock();
		var target = new Class1 { Clock = clock };

		StyleHelpers.TryAttach(style, target);

		CornerstoneTest.AreEqual(0.0, target.Double);

		target.Classes.Add("foo");
		clock.Step(TimeSpan.Zero);
		CornerstoneTest.AreEqual(5.0, target.Double);

		clock.Step(TimeSpan.FromSeconds(0.5));
		CornerstoneTest.AreEqual(7.5, target.Double);

		target.Classes.Remove("foo");
		CornerstoneTest.AreEqual(0.0, target.Double);
	}

	[PresentationTestMethod]
	public void InactiveBindingsShouldNotBeMadeActiveDuringStyleAttach()
	{
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<Class1>())
				{
					Setters =
					{
						new Setter(Class1.FooProperty, new Binding("Foo"))
					}
				},

				new Style(x => x.OfType<Class1>())
				{
					Setters =
					{
						new Setter(Class1.FooProperty, new Binding("Bar"))
					}
				}
			}
		};

		var values = new List<string>();
		var target = new Class1
		{
			DataContext = new
			{
				Foo = "Foo",
				Bar = "Bar"
			}
		};

		target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
		root.Child = target;

		CornerstoneTest.AreEqual(new[] { "foodefault", "Bar" }, values);
	}

	[PresentationTestMethod]
	public void InactiveBindingsShouldNotBeMadeActiveDuringStyleDetach()
	{
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<Class1>())
				{
					Setters =
					{
						new Setter(Class1.FooProperty, new Binding("Foo"))
					}
				},

				new Style(x => x.OfType<Class1>())
				{
					Setters =
					{
						new Setter(Class1.FooProperty, new Binding("Bar"))
					}
				}
			}
		};

		var target = new Class1
		{
			DataContext = new
			{
				Foo = "Foo",
				Bar = "Bar"
			}
		};

		root.Child = target;

		var values = new List<string>();
		target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
		root.Child = null;

		CornerstoneTest.AreEqual(new[] { "Bar", "foodefault" }, values);
	}

	[PresentationTestMethod]
	public void InactiveValuesShouldNotBeMadeActiveDuringStyleAttach()
	{
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<Class1>())
				{
					Setters =
					{
						new Setter(Class1.FooProperty, "Foo")
					}
				},

				new Style(x => x.OfType<Class1>())
				{
					Setters =
					{
						new Setter(Class1.FooProperty, "Bar")
					}
				}
			}
		};

		var values = new List<string>();
		var target = new Class1();

		target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
		root.Child = target;

		CornerstoneTest.AreEqual(new[] { "foodefault", "Bar" }, values);
	}

	[PresentationTestMethod]
	public void InactiveValuesShouldNotBeMadeActiveDuringStyleDetach()
	{
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<Class1>())
				{
					Setters =
					{
						new Setter(Class1.FooProperty, "Foo")
					}
				},

				new Style(x => x.OfType<Class1>())
				{
					Setters =
					{
						new Setter(Class1.FooProperty, "Bar")
					}
				}
			}
		};

		var target = new Class1();
		root.Child = target;

		var values = new List<string>();
		target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
		root.Child = null;

		CornerstoneTest.AreEqual(new[] { "Bar", "foodefault" }, values);
	}

	[PresentationTestMethod]
	public void InactiveValuesShouldNotBeMadeActiveDuringStyleDetach2()
	{
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<Class1>().Class("foo"))
				{
					Setters =
					{
						new Setter(Class1.FooProperty, "Foo")
					}
				},

				new Style(x => x.OfType<Class1>())
				{
					Setters =
					{
						new Setter(Class1.FooProperty, "Bar")
					}
				}
			}
		};

		var target = new Class1 { Classes = { "foo" } };
		root.Child = target;

		var values = new List<string>();
		target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));
		root.Child = null;

		CornerstoneTest.AreEqual(new[] { "Foo", "foodefault" }, values);
	}

	[PresentationTestMethod]
	public void InvalidatingStylesShouldDetachActivator()
	{
		var style = new Style(x => x.OfType<Class1>().Class("foo"))
		{
			Setters =
			{
				new Setter(Class1.FooProperty, "Foo")
			}
		};

		var target = new Class1();

		StyleHelpers.TryAttach(style, target);

		CornerstoneTest.AreEqual(1, target.Classes.ListenerCount);

		target.InvalidateStyles(false);

		CornerstoneTest.AreEqual(0, target.Classes.ListenerCount);
	}

	[PresentationTestMethod]
	public void LaterStylesShouldOverrideEarlier()
	{
		var styles = new Styles
		{
			new Style(x => x.OfType<Class1>().Class("foo"))
			{
				Setters =
				{
					new Setter(Class1.FooProperty, "Foo")
				}
			},

			new Style(x => x.OfType<Class1>().Class("foo"))
			{
				Setters =
				{
					new Setter(Class1.FooProperty, "Bar")
				}
			}
		};

		var target = new Class1();

		var values = new List<string>();
		target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));

		styles.TryAttach(target, null);
		target.Classes.Add("foo");
		target.Classes.Remove("foo");

		CornerstoneTest.AreEqual(new[] { "foodefault", "Bar", "foodefault" }, values);
	}

	[PresentationTestMethod]
	public void LaterStylesShouldOverrideEarlier2()
	{
		var styles = new Styles
		{
			new Style(x => x.OfType<Class1>().Class("foo"))
			{
				Setters =
				{
					new Setter(Class1.FooProperty, "Foo")
				}
			},

			new Style(x => x.OfType<Class1>().Class("bar"))
			{
				Setters =
				{
					new Setter(Class1.FooProperty, "Bar")
				}
			}
		};

		var target = new Class1();

		var values = new List<string>();
		target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));

		styles.TryAttach(target, null);
		target.Classes.Add("bar");
		target.Classes.Add("foo");
		target.Classes.Remove("foo");

		CornerstoneTest.AreEqual(new[] { "foodefault", "Bar" }, values);
	}

	[PresentationTestMethod]
	public void LaterStylesShouldOverrideEarlier3()
	{
		var styles = new Styles
		{
			new Style(x => x.OfType<Class1>().Class("foo"))
			{
				Setters =
				{
					new Setter(Class1.FooProperty, new Binding("Foo"))
				}
			},

			new Style(x => x.OfType<Class1>().Class("bar"))
			{
				Setters =
				{
					new Setter(Class1.FooProperty, new Binding("Bar"))
				}
			}
		};

		var target = new Class1
		{
			DataContext = new
			{
				Foo = "Foo",
				Bar = "Bar"
			}
		};

		var values = new List<string>();
		target.GetObservable(Class1.FooProperty).Subscribe(x => values.Add(x));

		styles.TryAttach(target, null);
		target.Classes.Add("bar");
		target.Classes.Add("foo");
		target.Classes.Remove("foo");

		CornerstoneTest.AreEqual(new[] { "foodefault", "Bar" }, values);
	}

	[PresentationTestMethod]
	public void LaterStylesShouldOverrideEarlier4()
	{
		var styles = new Styles
		{
			new Style(x => x.OfType<Class1>().Class("foo"))
			{
				Setters =
				{
					new Setter(Class1.FooProperty, "foo1")
				}
			},

			new Style(x => x.OfType<Class1>().Class("foo"))
			{
				Setters =
				{
					new Setter(Class1.FooProperty, "foo2"),
					new Setter(Class1.DoubleProperty, 123.4)
				}
			}
		};

		var target = new Class1();
		styles.TryAttach(target, null);
		target.Classes.Add("foo");

		CornerstoneTest.AreEqual("foo2", target.Foo);
		CornerstoneTest.AreEqual(123.4, target.Double);
	}

	[PresentationTestMethod]
	public void LaterStylesShouldOverrideEarlierWithBeginEndStyling()
	{
		var styles = new Styles
		{
			new Style(x => x.OfType<Class1>().Class("foo"))
			{
				Setters =
				{
					new Setter(Class1.FooProperty, "foo1"),
					new Setter(Class1.DoubleProperty, 123.4)
				}
			},

			new Style(x => x.OfType<Class1>().Class("foo").Class("bar"))
			{
				Setters =
				{
					new Setter(Class1.FooProperty, "foo2")
				}
			}
		};

		var target = new Class1();
		target.GetValueStore().BeginStyling();
		styles.TryAttach(target, null);
		target.GetValueStore().EndStyling();
		target.Classes.Add("bar");
		target.Classes.Add("foo");

		CornerstoneTest.AreEqual("foo2", target.Foo);
		CornerstoneTest.AreEqual(123.4, target.Double);

		target.Classes.Remove("foo");

		CornerstoneTest.AreEqual(0, target.Double);
	}

	[PresentationTestMethod]
	public void LocalValueShouldOverrideStyle()
	{
		var style = new Style(x => x.OfType<Class1>())
		{
			Setters =
			{
				new Setter(Class1.FooProperty, "Foo")
			}
		};

		var target = new Class1
		{
			Foo = "Original"
		};

		StyleHelpers.TryAttach(style, target);
		CornerstoneTest.AreEqual("Original", target.Foo);
	}

	[PresentationTestMethod]
	public void NestedOrStyleCanBeAdded()
	{
		var parent = new Style(x => x.OfType<Class1>());
		var nested = new Style(x => Selectors.Or(
			x.Nesting().Class("foo"),
			x.Nesting().Class("bar")));

		parent.Children.Add(nested);

		CornerstoneTest.Same(parent, nested.Parent);
	}

	[PresentationTestMethod]
	public void NestedStyleCanBeAdded()
	{
		var parent = new Style(x => x.OfType<Class1>());
		var nested = new Style(x => x.Nesting().Class("foo"));

		parent.Children.Add(nested);

		CornerstoneTest.Same(parent, nested.Parent);
	}

	[PresentationTestMethod]
	public void NestedStyleWithoutNestingOperatorThrows()
	{
		var parent = new Style(x => x.OfType<Class1>());
		var nested = new Style(x => x.Class("foo"));

		Assert.Throws<InvalidOperationException>(() => parent.Children.Add(nested));
	}

	[PresentationTestMethod]
	public void NestedStyleWithoutSelectorThrows()
	{
		var parent = new Style(x => x.OfType<Class1>());
		var nested = new Style();

		Assert.Throws<InvalidOperationException>(() => parent.Children.Add(nested));
	}

	[PresentationTestMethod]
	public void RemovingNestedStyleShouldDetachFromControl()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Styles =
			{
				new Styles
				{
					new Style(x => x.OfType<Border>())
					{
						Setters =
						{
							new Setter(Border.BorderThicknessProperty, new Thickness(4))
						}
					},
					new Style(x => x.OfType<Border>())
					{
						Setters =
						{
							new Setter(Border.BorderThicknessProperty, new Thickness(6))
						}
					}
				}
			},
			Child = border
		};

		root.Measure(Size.Infinity);
		CornerstoneTest.AreEqual(new Thickness(6), border.BorderThickness);

		((Styles) root.Styles[0]).RemoveAt(1);

		root.Measure(Size.Infinity);
		CornerstoneTest.AreEqual(new Thickness(4), border.BorderThickness);
	}

	[PresentationTestMethod]
	public void RemovingStyleShouldDetachFromControl()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<Border>())
				{
					Setters =
					{
						new Setter(Border.BorderThicknessProperty, new Thickness(4))
					}
				}
			},
			Child = border
		};

		root.Measure(Size.Infinity);
		CornerstoneTest.AreEqual(new Thickness(4), border.BorderThickness);

		root.Styles.RemoveAt(0);
		CornerstoneTest.AreEqual(new Thickness(0), border.BorderThickness);
	}

	[PresentationTestMethod]
	public void RemovingStyleWithNestedStyleShouldDetachFromControl()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Styles =
			{
				new Styles
				{
					new Style(x => x.OfType<Border>())
					{
						Setters =
						{
							new Setter(Border.BorderThicknessProperty, new Thickness(4))
						}
					}
				}
			},
			Child = border
		};

		root.Measure(Size.Infinity);
		CornerstoneTest.AreEqual(new Thickness(4), border.BorderThickness);

		root.Styles.RemoveAt(0);
		CornerstoneTest.AreEqual(new Thickness(0), border.BorderThickness);
	}

	[PresentationTestMethod]
	public void ShouldNotShareInstanceWhenOrSelectorIsPresent()
	{
		// Issue #13910
		var style = new Style(x => Selectors.Or(x.OfType<Class1>(), x.OfType<Class2>().Class("bar")))
		{
			Setters =
			{
				new Setter(Class1.FooProperty, "Foo")
			}
		};

		var target1 = new Class1 { Classes = { "foo" } };
		var target2 = new Class2();

		StyleHelpers.TryAttach(style, target1);
		StyleHelpers.TryAttach(style, target2);

		CornerstoneTest.AreEqual("Foo", target1.Foo);
		CornerstoneTest.AreEqual("foodefault", target2.Foo);
	}

	[PresentationTestMethod]
	public void ShouldSetOwnerOnAssignedResources()
	{
		var host = new StubResourceHost();
		var target = new Style();
		((IResourceProvider) target).AddOwner(host);

		var resources = new StubResourceDictionary();
		target.Resources = resources;

		resources.Calls.VerifyCalled("AddOwner", 1);
	}

	[PresentationTestMethod]
	public void ShouldSetOwnerOnAssignedResources2()
	{
		var host = new StubResourceHost();
		var target = new Style();

		var resources = new StubResourceDictionary();
		target.Resources = resources;

		host.Calls.Clear();
		((IResourceProvider) target).AddOwner(host);
		resources.Calls.VerifyCalled("AddOwner", 1);
	}

	[PresentationTestMethod]
	public void ShouldThrowForSelectorWithTrailingTemplateSelector()
	{
		Assert.Throws<InvalidOperationException>(() =>
			new Style(x => x.OfType<Button>().Template()));
	}

	[PresentationTestMethod]
	public void StyleShouldDetachWhenControlRemovedFromLogicalTree()
	{
		Border border;

		var style = new Style(x => x.OfType<Border>())
		{
			Setters =
			{
				new Setter(Border.BorderThicknessProperty, new Thickness(4))
			}
		};

		var root = new TestRoot
		{
			Child = border = new Border()
		};

		StyleHelpers.TryAttach(style, border);

		CornerstoneTest.AreEqual(new Thickness(4), border.BorderThickness);
		root.Child = null;
		CornerstoneTest.AreEqual(new Thickness(0), border.BorderThickness);
	}

	[PresentationTestMethod]
	public void StyleWithClassSelectorShouldUpdateAndRestoreValue()
	{
		var style = new Style(x => x.OfType<Class1>().Class("foo"))
		{
			Setters =
			{
				new Setter(Class1.FooProperty, "Foo")
			}
		};

		var target = new Class1();

		StyleHelpers.TryAttach(style, target);
		CornerstoneTest.AreEqual("foodefault", target.Foo);
		target.Classes.Add("foo");
		CornerstoneTest.AreEqual("Foo", target.Foo);
		target.Classes.Remove("foo");
		CornerstoneTest.AreEqual("foodefault", target.Foo);
	}

	[PresentationTestMethod]
	public void StyleWithClassSelectorShouldUpdateAndRestoreValueWithTemplateBinding()
	{
		var style = new Style(x => x.OfType<Class1>().Class("foo"))
		{
			Setters =
			{
				new Setter(Class1.FooProperty, "Foo")
			}
		};

		var templatedParent = new Class1 { Foo = "unset-foo" };
		var target = new Class1 { TemplatedParent = templatedParent };
		target.Bind(Class1.FooProperty, new TemplateBinding(Class1.FooProperty), BindingPriority.Template);

		StyleHelpers.TryAttach(style, target);
		CornerstoneTest.AreEqual("unset-foo", target.Foo);
		target.Classes.Add("foo");
		CornerstoneTest.AreEqual("Foo", target.Foo);
		target.Classes.Remove("foo");
		CornerstoneTest.AreEqual("unset-foo", target.Foo);
	}

	[PresentationTestMethod]
	public void StyleWithNoSelectorShouldApplyToContainingControl()
	{
		var style = new Style
		{
			Setters =
			{
				new Setter(Class1.FooProperty, "Foo")
			}
		};

		var target = new Class1();

		StyleHelpers.TryAttach(style, target);

		CornerstoneTest.AreEqual("Foo", target.Foo);
	}

	[PresentationTestMethod]
	public void StyleWithNoSelectorShouldNotApplyToOtherControl()
	{
		var style = new Style
		{
			Setters =
			{
				new Setter(Class1.FooProperty, "Foo")
			}
		};

		var target = new Class1();
		var other = new Class1();

		StyleHelpers.TryAttach(style, target, other);

		CornerstoneTest.AreEqual("foodefault", target.Foo);
	}

	[PresentationTestMethod]
	public void StyleWithOnlyTypeSelectorShouldUpdateValue()
	{
		var style = new Style(x => x.OfType<Class1>())
		{
			Setters =
			{
				new Setter(Class1.FooProperty, "Foo")
			}
		};

		var target = new Class1();

		StyleHelpers.TryAttach(style, target);

		CornerstoneTest.AreEqual("Foo", target.Foo);
	}

	[PresentationTestMethod]
	public void TemplateInInactiveStyleIsNotBuilt()
	{
		var instantiationCount = 0;
		var template = new FuncTemplate<Class1>(() =>
		{
			++instantiationCount;
			return new Class1();
		});

		var styles = new Styles
		{
			new Style(x => x.OfType<Class1>())
			{
				Setters =
				{
					new Setter(Class1.ChildProperty, template)
				}
			},

			new Style(x => x.OfType<Class1>())
			{
				Setters =
				{
					new Setter(Class1.ChildProperty, template)
				}
			}
		};

		var target = new Class1();
		target.GetValueStore().BeginStyling();
		styles.TryAttach(target, null);
		target.GetValueStore().EndStyling();

		CornerstoneTest.IsNotNull(target.Child);
		CornerstoneTest.AreEqual(1, instantiationCount);
	}

	[PresentationTestMethod]
	public void TemplateInNonMatchingStyleIsNotBuilt()
	{
		var instantiationCount = 0;
		var template = new FuncTemplate<Class1>(() =>
		{
			++instantiationCount;
			return new Class1();
		});

		var styles = new Styles
		{
			new Style(x => x.OfType<Class1>().Class("foo"))
			{
				Setters =
				{
					new Setter(Class1.ChildProperty, template)
				}
			},

			new Style(x => x.OfType<Class1>())
			{
				Setters =
				{
					new Setter(Class1.ChildProperty, template)
				}
			}
		};

		var target = new Class1();
		styles.TryAttach(target, null);

		CornerstoneTest.IsNotNull(target.Child);
		CornerstoneTest.AreEqual(1, instantiationCount);
	}

	#endregion

	#region Classes

	private class Class1 : Control
	{
		#region Fields

		public static readonly StyledProperty<Class1> ChildProperty =
			PresentationProperty.Register<Class1, Class1>(nameof(Child));

		public static readonly StyledProperty<double> DoubleProperty =
			PresentationProperty.Register<Class1, double>(nameof(Double));

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>(nameof(Foo), "foodefault");

		#endregion

		#region Properties

		public Class1 Child
		{
			get => GetValue(ChildProperty);
			set => SetValue(ChildProperty, value);
		}

		public double Double
		{
			get => GetValue(DoubleProperty);
			set => SetValue(DoubleProperty, value);
		}

		public string Foo
		{
			get => GetValue(FooProperty);
			set => SetValue(FooProperty, value);
		}

		#endregion

		#region Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	private class Class2 : Control
	{
		#region Fields

		public static readonly StyledProperty<string> FooProperty =
			Class1.FooProperty.AddOwner<Class2>();

		#endregion

		#region Properties

		public string Foo
		{
			get => GetValue(FooProperty);
			set => SetValue(FooProperty, value);
		}

		#endregion

		#region Methods

		protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
		{
			base.OnPropertyChanged(change);
		}

		#endregion
	}

	#endregion
}