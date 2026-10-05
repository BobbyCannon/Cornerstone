#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class TemplatedControlTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ApplyTemplateShouldCreateVisualChildren()
	{
		var target = new TemplatedControl
		{
			Template = new FuncControlTemplate((_, __) => new Decorator
			{
				Child = new Panel
				{
					Children =
					{
						new TextBlock(),
						new Border()
					}
				}
			})
		};

		target.ApplyTemplate();

		var types = target.GetVisualDescendants().Select(x => x.GetType()).ToList();

		CornerstoneTest.AreEqual(new[]
		{
			typeof(Decorator),
			typeof(Panel),
			typeof(TextBlock),
			typeof(Border)
		}, types);
		CornerstoneTest.Empty(target.GetLogicalChildren());
	}

	[PresentationTestMethod]
	public void ApplyTemplateShouldRaiseTemplateApplied()
	{
		var target = new TestTemplatedControl
		{
			Template = new FuncControlTemplate((_, __) => new Decorator())
		};

		var raised = false;

		target.TemplateApplied += (s, e) =>
		{
			CornerstoneTest.AreEqual(TemplatedControl.TemplateAppliedEvent, e.RoutedEvent);
			CornerstoneTest.Same(target, e.Source);
			CornerstoneTest.IsNotNull(e.NameScope);
			raised = true;
		};

		target.ApplyTemplate();

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void ApplyingNewTemplateClearsTemplatedParentOfOldTemplateChildren()
	{
		var target = new TestTemplatedControl
		{
			Template = new FuncControlTemplate((_, __) => new Decorator
			{
				Child = new Border()
			})
		};

		target.ApplyTemplate();

		var decorator = (Decorator) target.GetVisualChildren().Single();
		var border = (Border) decorator.Child!;

		CornerstoneTest.AreEqual(target, decorator.TemplatedParent);
		CornerstoneTest.AreEqual(target, border.TemplatedParent);

		target.Template = new FuncControlTemplate((_, __) => new Canvas());

		// Templated children should not be removed here: the control may be re-added
		// somewhere with the same template, so they could still be of use.
		CornerstoneTest.Same(decorator, target.GetVisualChildren().Single());
		CornerstoneTest.AreEqual(target, decorator.TemplatedParent);
		CornerstoneTest.AreEqual(target, border.TemplatedParent);

		target.ApplyTemplate();

		CornerstoneTest.IsNull(decorator.TemplatedParent);
		CornerstoneTest.IsNull(border.TemplatedParent);
	}

	[PresentationTestMethod]
	public void ChangingResourceInTemplatedParentShouldAffectTemplatedChild()
	{
		var target = new ContentControl
		{
			Resources =
			{
				{ "red", Brushes.Red }
			},
			Template = new FuncControlTemplate<ContentControl>((x, scope) =>
			{
				var result = new ContentPresenter
				{
					Name = "PART_ContentPresenter",
					[!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty]
				}.RegisterInNameScope(scope);

				result.Bind(ContentPresenter.BackgroundProperty, result.GetResourceObservable("red"));

				return result;
			})
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();

		var contentPresenter = CornerstoneTest.IsType<ContentPresenter>(target.GetVisualChildren().Single());
		CornerstoneTest.Same(Brushes.Red, contentPresenter.Background);

		target.Resources["red"] = Brushes.Green;

		CornerstoneTest.Same(Brushes.Green, contentPresenter.Background);
	}

	[PresentationTestMethod]
	public void ChangingTemplateShouldClearOldTemplatedChildsParent()
	{
		var target = new TemplatedControl
		{
			Template = new FuncControlTemplate((_, __) => new Decorator())
		};

		target.ApplyTemplate();

		var child = (Decorator) target.GetVisualChildren().Single();

		target.Template = new FuncControlTemplate((_, __) => new Canvas());
		target.ApplyTemplate();

		CornerstoneTest.IsNull(child.Parent);
	}

	[PresentationTestMethod]
	public void MovingToNewLogicalTreeShouldDetachAttachTemplateChild()
	{
		TestTemplatedControl target;
		var root = new TestRoot
		{
			Child = target = new TestTemplatedControl
			{
				Template = new FuncControlTemplate((_, __) => new Decorator())
			}
		};

		CornerstoneTest.IsNotNull(target.Template);
		target.ApplyTemplate();

		var templateChild = (ILogical) target.GetVisualChildren().Single();
		CornerstoneTest.IsTrue(templateChild.IsAttachedToLogicalTree);

		root.Child = null;
		CornerstoneTest.IsFalse(templateChild.IsAttachedToLogicalTree);

		var newRoot = new TestRoot { Child = target };
		CornerstoneTest.IsTrue(templateChild.IsAttachedToLogicalTree);
	}

	[PresentationTestMethod]
	public void NestedTemplatedControlShouldNotHaveTemplateApplied()
	{
		var target = new TemplatedControl
		{
			Template = new FuncControlTemplate((_, __) => new ScrollViewer())
		};

		target.ApplyTemplate();

		var child = (ScrollViewer) target.GetVisualChildren().Single();
		CornerstoneTest.Empty(child.GetVisualChildren());
	}

	[PresentationTestMethod]
	public void NestedTemplatedControlsHaveCorrectTemplatedParent()
	{
		var target = new TestTemplatedControl
		{
			Template = new FuncControlTemplate((_, __) =>
			{
				return new ContentControl
				{
					Template = new FuncControlTemplate((parent, ___) =>
					{
						return new Border
						{
							Child = new ContentPresenter
							{
								[~ContentPresenter.ContentProperty] = parent.GetObservable(ContentControl.ContentProperty).ToBinding()
							}
						};
					}),
					Content = new Decorator
					{
						Child = new TextBlock()
					}
				};
			})
		};

		target.ApplyTemplate();

		var contentControl = target.GetTemplateDescendants().OfType<ContentControl>().Single();
		contentControl.ApplyTemplate();

		var border = contentControl.GetTemplateDescendants().OfType<Border>().Single();
		var presenter = contentControl.GetTemplateDescendants().OfType<ContentPresenter>().Single();
		var decorator = (Decorator) presenter.Content!;
		var textBlock = (TextBlock) decorator.Child!;

		CornerstoneTest.AreEqual(target, contentControl.TemplatedParent);
		CornerstoneTest.AreEqual(contentControl, border.TemplatedParent);
		CornerstoneTest.AreEqual(contentControl, presenter.TemplatedParent);
		CornerstoneTest.AreEqual(target, decorator.TemplatedParent);
		CornerstoneTest.AreEqual(target, textBlock.TemplatedParent);
	}

	[PresentationTestMethod]
	public void ReaddingToDifferentLogicalTreeShouldRecreateTemplate()
	{
		TestTemplatedControl target;

		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<TestTemplatedControl>())
				{
					Setters =
					{
						new Setter(
							TemplatedControl.TemplateProperty,
							new FuncControlTemplate((_, __) => new Decorator
							{
								Child = new Border()
							}))
					}
				}
			},
			Child = target = new TestTemplatedControl()
		};

		var root2 = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<TestTemplatedControl>())
				{
					Setters =
					{
						new Setter(
							TemplatedControl.TemplateProperty,
							new FuncControlTemplate((_, __) => new Decorator
							{
								Child = new Border()
							}))
					}
				}
			}
		};

		CornerstoneTest.IsNotNull(target.Template);
		target.ApplyTemplate();

		var expected = (Decorator) target.GetVisualChildren().Single();

		root.Child = null;
		root2.Child = target;
		target.ApplyTemplate();

		var child = target.GetVisualChildren().Single();
		CornerstoneTest.IsNotNull(target.Template);
		CornerstoneTest.IsNotNull(child);
		CornerstoneTest.NotSame(expected, child);
	}

	[PresentationTestMethod]
	public void ReaddingToSameLogicalTreeShouldNotRecreateTemplate()
	{
		TestTemplatedControl target;
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<TestTemplatedControl>())
				{
					Setters =
					{
						new Setter(
							TemplatedControl.TemplateProperty,
							new FuncControlTemplate((_, __) => new Decorator
							{
								Child = new Border()
							}))
					}
				}
			},
			Child = target = new TestTemplatedControl()
		};

		CornerstoneTest.IsNotNull(target.Template);
		target.ApplyTemplate();
		var expected = (Decorator) target.GetVisualChildren().Single();

		root.Child = null;
		root.Child = target;
		target.ApplyTemplate();

		CornerstoneTest.Same(expected, target.GetVisualChildren().Single());
	}

	[PresentationTestMethod]
	public void RemovingFromLogicalTreeShouldNotRemoveChild()
	{
		var templateChild = new Border();
		TestTemplatedControl target;
		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.OfType<TestTemplatedControl>())
				{
					Setters =
					{
						new Setter(
							TemplatedControl.TemplateProperty,
							new FuncControlTemplate((_, __) => new Decorator
							{
								Child = new Border()
							}))
					}
				}
			},
			Child = target = new TestTemplatedControl()
		};

		CornerstoneTest.IsNotNull(target.Template);
		target.ApplyTemplate();

		root.Child = null;

		CornerstoneTest.IsNull(target.Template);
		CornerstoneTest.IsType<Decorator>(target.GetVisualChildren().Single());
	}

	[PresentationTestMethod]
	public void TemplateChildAttachedToLogicalTreeShouldBeRaised()
	{
		var templateChild = new Border();
		var root = new TestRoot
		{
			Child = new TestTemplatedControl
			{
				Template = new FuncControlTemplate((_, __) => new Decorator
				{
					Child = templateChild
				})
			}
		};

		var raised = false;
		templateChild.AttachedToLogicalTree += (s, e) => raised = true;

		root.Child.ApplyTemplate();
		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void TemplateChildDetachedFromLogicalTreeShouldBeRaised()
	{
		var templateChild = new Border();
		var root = new TestRoot
		{
			Child = new TestTemplatedControl
			{
				Template = new FuncControlTemplate((_, __) => new Decorator
				{
					Child = templateChild
				})
			}
		};

		root.Child.ApplyTemplate();

		var raised = false;
		templateChild.DetachedFromLogicalTree += (s, e) => raised = true;

		root.Child = null;
		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void TemplateDoesntGetExecutedOnSet()
	{
		var executed = false;

		var template = new FuncControlTemplate((_, __) =>
		{
			executed = true;
			return new Control();
		});

		var target = new TemplatedControl
		{
			Template = template
		};

		CornerstoneTest.IsFalse(executed);
	}

	[PresentationTestMethod]
	public void TemplateGetsExecutedOnMeasure()
	{
		var executed = false;

		var template = new FuncControlTemplate((_, __) =>
		{
			executed = true;
			return new Control();
		});

		var target = new TemplatedControl
		{
			Template = template
		};

		target.Measure(new Size(100, 100));

		CornerstoneTest.IsTrue(executed);
	}

	[PresentationTestMethod]
	public void TemplatedChildShouldFindResourceInTemplatedParent()
	{
		var target = new ContentControl
		{
			Resources =
			{
				{ "red", Brushes.Red }
			},
			Template = new FuncControlTemplate<ContentControl>((x, scope) =>
			{
				var result = new ContentPresenter
				{
					Name = "PART_ContentPresenter",
					[!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty]
				}.RegisterInNameScope(scope);

				result.Bind(ContentPresenter.BackgroundProperty, result.GetResourceObservable("red"));

				return result;
			})
		};

		var root = new TestRoot(target);
		target.ApplyTemplate();

		var contentPresenter = CornerstoneTest.IsType<ContentPresenter>(target.GetVisualChildren().Single());
		CornerstoneTest.Same(Brushes.Red, contentPresenter.Background);
	}

	[PresentationTestMethod]
	public void TemplatedChildShouldHaveParentSet()
	{
		var target = new TemplatedControl
		{
			Template = new FuncControlTemplate((_, __) => new Decorator())
		};

		target.ApplyTemplate();

		var child = (Decorator) target.GetVisualChildren().Single();

		CornerstoneTest.AreEqual(target, child.Parent);
		CornerstoneTest.AreEqual(target, child.GetLogicalParent());
	}

	[PresentationTestMethod]
	public void TemplatedChildrenShouldBeStyled()
	{
		TestTemplatedControl target;

		var root = new TestRoot
		{
			Styles =
			{
				new Style(x => x.Is<Control>())
				{
					Setters =
					{
						new Setter(Control.TagProperty, "foo")
					}
				}
			},
			Child = target = new TestTemplatedControl
			{
				Template = new FuncControlTemplate((_, __) =>
				{
					return new StackPanel
					{
						Children =
						{
							new TextBlock()
						}
					};
				})
			}
		};

		target.ApplyTemplate();

		foreach (var child in target.GetTemplateDescendants().OfType<Control>())
		{
			CornerstoneTest.AreEqual("foo", child.Tag);
		}
	}

	[PresentationTestMethod]
	public void TemplatedChildrenShouldHaveTemplatedParentSet()
	{
		var target = new TemplatedControl
		{
			Template = new FuncControlTemplate((_, __) => new Decorator
			{
				Child = new Panel
				{
					Children =
					{
						new TextBlock(),
						new Border()
					}
				}
			})
		};

		target.ApplyTemplate();

		var templatedParents = target.GetVisualDescendants()
			.OfType<Control>()
			.Select(x => x.TemplatedParent)
			.ToList();

		CornerstoneTest.AreEqual(4, templatedParents.Count);
		CornerstoneTest.IsTrue(templatedParents.All(x => x == target));
	}

	[PresentationTestMethod]
	public void TemplatedControlLetterSpacingCanBeNegative()
	{
		var target = new TestTemplatedControl { LetterSpacing = -1.5 };
		CornerstoneTest.AreEqual(-1.5, target.LetterSpacing);
	}

	[PresentationTestMethod]
	public void TemplatedControlLetterSpacingCanBeSetAndRetrieved()
	{
		var target = new TestTemplatedControl { LetterSpacing = 2.5 };
		CornerstoneTest.AreEqual(2.5, target.LetterSpacing);
	}

	[PresentationTestMethod]
	public void TemplatedControlLetterSpacingDefaultValueIsZero()
	{
		var target = new TestTemplatedControl();
		CornerstoneTest.AreEqual(0, target.LetterSpacing);
	}

	[PresentationTestMethod]
	public void TemplatedControlLetterSpacingInheritsToContentPresenter()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new ContentControl
			{
				LetterSpacing = 3.0,
				Content = "Test"
			};
			var root = new TestRoot { Child = target };

			target.ApplyTemplate();

			var presenter = target.Presenter;
			CornerstoneTest.IsNotNull(presenter);
			CornerstoneTest.AreEqual(3.0, presenter.LetterSpacing);
		}
	}

	[PresentationTestMethod]
	public void TemplatedControlLetterSpacingUsesTextElementProperty()
	{
		CornerstoneTest.Same(TextElement.LetterSpacingProperty, TemplatedControl.LetterSpacingProperty);
	}

	[PresentationTestMethod]
	public void TypefaceIsCachedUntilFontPropertiesChange()
	{
		var target = new TemplatedControl();
		var first = target.Typeface;
		CornerstoneTest.AreEqual(first, target.Typeface);

		target.FontWeight = FontWeight.Bold;
		target.FontStyle = FontStyle.Italic;

		var afterChange = target.Typeface;
		CornerstoneTest.AreEqual(FontWeight.Bold, afterChange.Weight);
		CornerstoneTest.AreEqual(FontStyle.Italic, afterChange.Style);
		CornerstoneTest.AreNotEqual(first, afterChange);
	}

	private static Control ScrollViewerTemplate(ScrollViewer control, INameScope scope)
	{
		var result = new ScrollContentPresenter
		{
			Name = "PART_ContentPresenter",
			[~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty]
		}.RegisterInNameScope(scope);

		return result;
	}

	private static Control ScrollingContentControlTemplate(ContentControl control, INameScope scope)
	{
		return new Border
		{
			Child = new ScrollViewer
			{
				Template = new FuncControlTemplate<ScrollViewer>(ScrollViewerTemplate),
				Name = "ScrollViewer",
				Content = new ContentPresenter
				{
					Name = "PART_ContentPresenter",
					[!ContentPresenter.ContentProperty] = control[!ContentControl.ContentProperty]
				}.RegisterInNameScope(scope)
			}.RegisterInNameScope(scope)
		};
	}

	#endregion
}