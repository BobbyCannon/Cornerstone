#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class StyledElementTestsTheming : ScopedTestBase
{
	#region Methods

	private static ControlTheme CreateDerivedTheme()
	{
		return new ControlTheme
		{
			TargetType = typeof(ThemedControl),
			BasedOn = CreateTheme(),
			Setters =
			{
				new Setter(Border.BorderBrushProperty, Brushes.Blue)
			},
			Children =
			{
				new Style(x => x.Nesting().Template().OfType<Border>())
				{
					Setters = { new Setter(Border.BorderBrushProperty, Brushes.Yellow) }
				},
				new Style(x => x.Nesting().Class("foo").Template().OfType<Border>())
				{
					Setters = { new Setter(Border.BorderBrushProperty, Brushes.Cyan) }
				}
			}
		};
	}

	private static ControlTheme CreateTheme(string tag = "theme")
	{
		var template = new FuncControlTemplate<ThemedControl>((o, n) => new Border { Child = new Border() });

		return new ControlTheme
		{
			TargetType = typeof(ThemedControl),
			Setters =
			{
				new Setter(Control.TagProperty, tag),
				new Setter(TemplatedControl.TemplateProperty, template),
				new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(5))
			},
			Children =
			{
				new Style(x => x.Nesting().Template().OfType<Border>())
				{
					Setters =
					{
						new Setter(Border.BackgroundProperty, Brushes.Red),
						new Setter(Control.TagProperty, tag)
					}
				},
				new Style(x => x.Nesting().Class("foo").Template().OfType<Border>())
				{
					Setters = { new Setter(Border.BackgroundProperty, Brushes.Green) }
				}
			}
		};
	}

	#endregion

	#region Classes

	[TestClass]
	public class ImplicitTheme : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void CanAttachThenReattachToSameLogicalTree()
		{
			var target = CreateTarget();
			var root = CreateRoot(target);

			CornerstoneTest.AreEqual("theme", target.Tag);

			root.Child = null;
			root.Child = target;

			CornerstoneTest.AreEqual("theme", target.Tag);
		}

		[PresentationTestMethod]
		public void ImplicitThemeIsAppliedWhenAttachedToLogicalTree()
		{
			var target = CreateTarget();
			CreateRoot(target);
			CornerstoneTest.IsNotNull(target.Template);

			var border = CornerstoneTest.IsType<Border>(target.VisualChild);
			CornerstoneTest.AreEqual(Brushes.Red, border.Background);

			target.Classes.Add("foo");
			CornerstoneTest.AreEqual(Brushes.Green, border.Background);
		}

		[PresentationTestMethod]
		public void ImplicitThemeIsNotDetachedWhenRemovedFromLogicalTree()
		{
			var target = CreateTarget();
			var root = CreateRoot(target);

			CornerstoneTest.AreEqual("theme", target.Tag);

			root.Child = null;

			var border = CornerstoneTest.IsType<Border>(target.VisualChild);
			CornerstoneTest.AreEqual("theme", target.Tag);
			CornerstoneTest.AreEqual("theme", border.Tag);
		}

		[PresentationTestMethod]
		public void ImplicitThemeIsReevaluatedWhenRemovedAndAddedToDifferentLogicalTree()
		{
			var target = CreateTarget();
			var root1 = CreateRoot(target, "theme1");
			var root2 = CreateRoot(null, "theme2");

			CornerstoneTest.AreEqual("theme1", target.Tag);

			root1.Child = null;
			root2.Child = target;

			var border = CornerstoneTest.IsType<Border>(target.VisualChild);
			CornerstoneTest.AreEqual("theme2", target.Tag);
			CornerstoneTest.AreEqual("theme2", border.Tag);
		}

		[PresentationTestMethod]
		public void NestedStyleCanOverridePropertyInInnerTemplatedControl()
		{
			var target = new ThemedControl2
			{
				Theme = new ControlTheme(typeof(ThemedControl2))
				{
					Setters =
					{
						new Setter(
							TemplatedControl.TemplateProperty,
							new FuncControlTemplate<ThemedControl2>((o, n) => new ThemedControl()))
					},
					Children =
					{
						new Style(x => x.Nesting().Template().OfType<ThemedControl>())
						{
							Setters = { new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(7)) }
						}
					}
				}
			};

			var root = CreateRoot(target);
			var inner = CornerstoneTest.IsType<ThemedControl>(target.VisualChild);

			CornerstoneTest.AreEqual(new CornerRadius(7), inner.CornerRadius);
		}

		private static TestRoot CreateRoot(Control child, string themeTag = "theme")
		{
			var result = new TestRoot();
			result.Resources.Add(typeof(ThemedControl), CreateTheme(themeTag));
			result.Child = child;
			result.LayoutManager.ExecuteInitialLayoutPass();
			return result;
		}

		private static ThemedControl CreateTarget()
		{
			return new();
		}

		#endregion
	}

	[TestClass]
	public class InlineTheme : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void BasedOnThemeIsAppliedWhenAttachedToLogicalTree()
		{
			var target = CreateTarget(CreateDerivedTheme());

			CornerstoneTest.IsNull(target.Template);

			CreateRoot(target);
			CornerstoneTest.IsNotNull(target.Template);
			CornerstoneTest.AreEqual(Brushes.Blue, target.BorderBrush);

			var border = CornerstoneTest.IsType<Border>(target.VisualChild);
			CornerstoneTest.AreEqual(Brushes.Red, border.Background);
			CornerstoneTest.AreEqual(Brushes.Yellow, border.BorderBrush);

			target.Classes.Add("foo");
			CornerstoneTest.AreEqual(Brushes.Green, border.Background);
			CornerstoneTest.AreEqual(Brushes.Cyan, border.BorderBrush);
		}

		[PresentationTestMethod]
		public void PrimaryThemeIsNotDetachedFromTemplateControlsWhenThemePropertyCleared()
		{
			var templatedParentTheme = new ControlTheme
			{
				TargetType = typeof(ThemedControl),
				Children =
				{
					new Style(x => x.Nesting().Template().OfType<Button>())
					{
						Setters =
						{
							new Setter(Panel.BackgroundProperty, Brushes.Red)
						}
					}
				}
			};

			var childTheme = new ControlTheme
			{
				TargetType = typeof(Button),
				Setters =
				{
					new Setter(TemplatedControl.ForegroundProperty, Brushes.Green)
				}
			};

			var target = CreateTarget(templatedParentTheme);
			target.Template = new FuncControlTemplate<ThemedControl>((o, n) => new Button
			{
				Theme = childTheme
			});

			var root = CreateRoot(target, true);

			var templateChild = CornerstoneTest.IsType<Button>(target.VisualChild);
			CornerstoneTest.AreEqual(Brushes.Red, templateChild.Background);
			CornerstoneTest.AreEqual(Brushes.Green, templateChild.Foreground);

			target.Theme = null;

			CornerstoneTest.IsNull(templateChild.Background);
			CornerstoneTest.AreEqual(Brushes.Green, templateChild.Foreground);
		}

		[PresentationTestMethod]
		public void SettingExplicitThemeDetachesDefaultTheme()
		{
			var target = new ThemedControl();
			var root = new TestRoot
			{
				Resources = { { typeof(ThemedControl), CreateTheme() } },
				Child = target
			};

			root.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.AreEqual("theme", target.Tag);

			target.Theme = new ControlTheme(typeof(ThemedControl))
			{
				Setters =
				{
					new Setter(ThemedControl.BackgroundProperty, Brushes.Yellow)
				}
			};

			root.LayoutManager.ExecuteLayoutPass();

			CornerstoneTest.IsNull(target.Tag);
			CornerstoneTest.AreEqual(Brushes.Yellow, target.Background);
		}

		[PresentationTestMethod]
		public void TemplatedParentThemeIsDetachedFromTemplateControlsWhenThemePropertyCleared()
		{
			var theme = new ControlTheme
			{
				TargetType = typeof(ThemedControl),
				Children =
				{
					new Style(x => x.Nesting().Template().OfType<Canvas>())
					{
						Setters =
						{
							new Setter(Panel.BackgroundProperty, Brushes.Red)
						}
					}
				}
			};

			var target = CreateTarget(theme);
			target.Template = new FuncControlTemplate<ThemedControl>((o, n) => new Canvas());

			var root = CreateRoot(target);

			var canvas = CornerstoneTest.IsType<Canvas>(target.VisualChild);
			CornerstoneTest.AreEqual(Brushes.Red, canvas.Background);

			target.Theme = null;

			CornerstoneTest.Same(canvas, target.VisualChild);
			CornerstoneTest.IsNull(canvas.Background);
		}

		[PresentationTestMethod]
		public void TemplatedParentThemeIsNotDetachedFromTemplateControlsWhenPrimaryThemePropertyCleared()
		{
			var templatedParentTheme = new ControlTheme
			{
				TargetType = typeof(ThemedControl),
				Children =
				{
					new Style(x => x.Nesting().Template().OfType<Button>())
					{
						Setters =
						{
							new Setter(Panel.BackgroundProperty, Brushes.Red)
						}
					}
				}
			};

			var childTheme = new ControlTheme
			{
				TargetType = typeof(Button),
				Setters =
				{
					new Setter(Button.TagProperty, "childTheme")
				}
			};

			var target = CreateTarget(templatedParentTheme);
			target.Template = new FuncControlTemplate<ThemedControl>((o, n) => new Button
			{
				Theme = childTheme
			});

			var root = CreateRoot(target, true);

			var templateChild = CornerstoneTest.IsType<Button>(target.VisualChild);
			CornerstoneTest.AreEqual(Brushes.Red, templateChild.Background);
			CornerstoneTest.AreEqual("childTheme", templateChild.Tag);

			templateChild.Theme = null;

			CornerstoneTest.AreEqual(Brushes.Red, templateChild.Background);
			CornerstoneTest.IsNull(templateChild.Tag);
		}

		[PresentationTestMethod]
		public void ThemeHasLowerPriorityThanStyle()
		{
			var target = CreateTarget();
			CreateRoot(target, true);

			CornerstoneTest.AreEqual("style", target.Tag);
		}

		[PresentationTestMethod]
		public void ThemeHasLowerPriorityThanStyleAfterChange()
		{
			var target = CreateTarget();
			var theme = target.Theme;
			CreateRoot(target, true);

			target.Theme = null;
			target.Theme = theme;
			target.ApplyStyling();

			CornerstoneTest.AreEqual("style", target.Tag);
		}

		[PresentationTestMethod]
		public void ThemeIsAppliedOnLayoutAfterThemePropertyChanges()
		{
			var target = new ThemedControl();
			var root = CreateRoot(target);

			CornerstoneTest.IsNull(target.Template);

			target.Theme = CreateTheme();
			CornerstoneTest.IsNull(target.Template);

			root.LayoutManager.ExecuteLayoutPass();

			var border = CornerstoneTest.IsType<Border>(target.VisualChild);
			CornerstoneTest.IsNotNull(target.Template);
			CornerstoneTest.AreEqual(Brushes.Red, border.Background);
		}

		[PresentationTestMethod]
		public void ThemeIsAppliedToDerivedClassWhenAttachedToLogicalTree()
		{
			var target = new DerivedThemedControl
			{
				Theme = CreateTheme()
			};

			CornerstoneTest.IsNull(target.Template);

			CreateRoot(target);
			CornerstoneTest.IsNotNull(target.Template);

			var border = CornerstoneTest.IsType<Border>(target.VisualChild);
			CornerstoneTest.AreEqual(Brushes.Red, border.Background);

			target.Classes.Add("foo");
			CornerstoneTest.AreEqual(Brushes.Green, border.Background);
		}

		[PresentationTestMethod]
		public void ThemeIsAppliedWhenAttachedToLogicalTree()
		{
			var target = CreateTarget();

			CornerstoneTest.IsNull(target.Template);

			CreateRoot(target);
			CornerstoneTest.IsNotNull(target.Template);

			var border = CornerstoneTest.IsType<Border>(target.VisualChild);
			CornerstoneTest.AreEqual(Brushes.Red, border.Background);

			target.Classes.Add("foo");
			CornerstoneTest.AreEqual(Brushes.Green, border.Background);
		}

		[PresentationTestMethod]
		public void ThemeIsDetachedWhenThemePropertyCleared()
		{
			var target = CreateTarget();
			CreateRoot(target);

			CornerstoneTest.IsNotNull(target.Template);

			target.Theme = null;
			CornerstoneTest.IsNull(target.Template);
		}

		[PresentationTestMethod]
		public void UnrelatedStylesAreNotDetachedFromTemplateControlsWhenThemePropertyCleared()
		{
			var target = CreateTarget();
			var root = CreateRoot(target, true);

			var canvas = CornerstoneTest.IsType<Border>(target.VisualChild);
			CornerstoneTest.AreEqual("style", canvas.Tag);

			target.Theme = null;

			CornerstoneTest.Same(canvas, target.VisualChild);
			CornerstoneTest.AreEqual("style", canvas.Tag);
		}

		[PresentationTestMethod]
		public void UnrelatedStylesAreNotDetachedWhenThemePropertyCleared()
		{
			var target = CreateTarget();
			CreateRoot(target, true);

			CornerstoneTest.AreEqual("style", target.Tag);

			target.Theme = null;
			CornerstoneTest.AreEqual("style", target.Tag);
		}

		private static TestRoot CreateRoot(
			Control child,
			bool createAdditionalStyles = false)
		{
			var result = new TestRoot();

			if (createAdditionalStyles)
			{
				result.Styles.Add(new Style(x => x.OfType<ThemedControl>())
				{
					Setters =
					{
						new Setter(Control.TagProperty, "style")
					}
				});

				result.Styles.Add(new Style(x => x.OfType<Border>())
				{
					Setters =
					{
						new Setter(Control.TagProperty, "style")
					}
				});
			}

			result.Child = child;
			result.LayoutManager.ExecuteInitialLayoutPass();
			return result;
		}

		private static ThemedControl CreateTarget(ControlTheme theme = null)
		{
			return new ThemedControl
			{
				Theme = theme ?? CreateTheme()
			};
		}

		#endregion
	}

	[TestClass]
	public class ThemeFromStyle : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void TemplatedParentThemeChangeAppliesRecursivelyToVisualChildren()
		{
			var theme = CreateDerivedTheme();
			var target = CreateTarget();

			CornerstoneTest.IsNull(target.Theme);
			CornerstoneTest.IsNull(target.Template);

			var root = CreateRoot(target, theme.BasedOn);

			CornerstoneTest.IsNotNull(target.Theme);
			CornerstoneTest.IsNotNull(target.Template);

			root.Styles.Add(new Style(x => x.OfType<ThemedControl>().Class("foo"))
			{
				Setters = { new Setter(StyledElement.ThemeProperty, theme) }
			});

			root.LayoutManager.ExecuteLayoutPass();

			var border = CornerstoneTest.IsType<Border>(target.VisualChild);
			var inner = CornerstoneTest.IsType<Border>(border.Child);

			CornerstoneTest.AreEqual(Brushes.Red, border.Background);
			CornerstoneTest.AreEqual(Brushes.Red, inner.Background);

			CornerstoneTest.AreEqual(null, inner.BorderBrush);
			CornerstoneTest.AreEqual(null, inner.BorderBrush);

			target.Classes.Add("foo");
			root.LayoutManager.ExecuteLayoutPass();

			CornerstoneTest.AreEqual(Brushes.Green, border.Background);
			CornerstoneTest.AreEqual(Brushes.Green, inner.Background);

			CornerstoneTest.AreEqual(Brushes.Cyan, inner.BorderBrush);
			CornerstoneTest.AreEqual(Brushes.Cyan, inner.BorderBrush);
		}

		[PresentationTestMethod]
		public void TemplatedParentThemeChangeAppliesToChildren()
		{
			var theme = CreateDerivedTheme();
			var target = CreateTarget();

			CornerstoneTest.IsNull(target.Theme);
			CornerstoneTest.IsNull(target.Template);

			var root = CreateRoot(target, theme.BasedOn);

			CornerstoneTest.IsNotNull(target.Theme);
			CornerstoneTest.IsNotNull(target.Template);

			root.Styles.Add(new Style(x => x.OfType<ThemedControl>().Class("foo"))
			{
				Setters = { new Setter(StyledElement.ThemeProperty, theme) }
			});

			root.LayoutManager.ExecuteLayoutPass();

			var border = CornerstoneTest.IsType<Border>(target.VisualChild);
			CornerstoneTest.AreEqual(Brushes.Red, border.Background);

			target.Classes.Add("foo");
			root.LayoutManager.ExecuteLayoutPass();

			CornerstoneTest.AreEqual(Brushes.Green, border.Background);
		}

		[PresentationTestMethod]
		public void ThemeCanBeChangedByStyleClass()
		{
			var target = CreateTarget();
			var theme1 = CreateTheme();
			var theme2 = new ControlTheme(typeof(ThemedControl));
			var root = new TestRoot
			{
				Styles =
				{
					new Style(x => x.OfType<ThemedControl>())
					{
						Setters = { new Setter(StyledElement.ThemeProperty, theme1) }
					},
					new Style(x => x.OfType<ThemedControl>().Class("bar"))
					{
						Setters = { new Setter(StyledElement.ThemeProperty, theme2) }
					}
				}
			};

			root.Child = target;
			root.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.Same(theme1, target.Theme);
			CornerstoneTest.IsNotNull(target.Template);

			target.Classes.Add("bar");
			CornerstoneTest.Same(theme2, target.Theme);
			CornerstoneTest.IsNull(target.Template);
		}

		[PresentationTestMethod]
		public void ThemeCanBeSetToLocalValueWhileUpdatingDueToStyleClass()
		{
			var target = CreateTarget();
			var theme1 = CreateTheme();
			var theme2 = new ControlTheme(typeof(ThemedControl));
			var theme3 = new ControlTheme(typeof(ThemedControl));
			var root = new TestRoot
			{
				Styles =
				{
					new Style(x => x.OfType<ThemedControl>())
					{
						Setters = { new Setter(StyledElement.ThemeProperty, theme1) }
					},
					new Style(x => x.OfType<ThemedControl>().Class("bar"))
					{
						Setters = { new Setter(StyledElement.ThemeProperty, theme2) }
					}
				}
			};

			root.Child = target;
			root.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.Same(theme1, target.Theme);
			CornerstoneTest.IsNotNull(target.Template);

			target.Classes.Add("bar");

			// At this point, theme2 has been promoted to a local value internally in StyledElement;
			// make sure that setting a new local value here doesn't cause it to be cleared when we
			// do a layout pass because StyledElement thinks its clearing the promoted theme.
			target.Theme = theme3;

			root.LayoutManager.ExecuteLayoutPass();

			CornerstoneTest.Same(target.Theme, theme3);
		}

		[PresentationTestMethod]
		public void ThemeIsAppliedWhenAttachedToLogicalTree()
		{
			var target = CreateTarget();

			CornerstoneTest.IsNull(target.Theme);
			CornerstoneTest.IsNull(target.Template);

			CreateRoot(target);

			CornerstoneTest.IsNotNull(target.Theme);
			CornerstoneTest.IsNotNull(target.Template);

			var border = CornerstoneTest.IsType<Border>(target.VisualChild);
			CornerstoneTest.AreEqual(Brushes.Red, border.Background);

			target.Classes.Add("foo");
			CornerstoneTest.AreEqual(Brushes.Green, border.Background);
		}

		private static TestRoot CreateRoot(Control child, ControlTheme theme = null)
		{
			var result = new TestRoot
			{
				Styles =
				{
					new Style(x => x.OfType<ThemedControl>())
					{
						Setters = { new Setter(StyledElement.ThemeProperty, theme ?? CreateTheme()) }
					}
				}
			};

			result.Child = child;
			result.LayoutManager.ExecuteInitialLayoutPass();
			return result;
		}

		private static ThemedControl CreateTarget()
		{
			return new ThemedControl();
		}

		#endregion
	}

	private class DerivedThemedControl : ThemedControl
	{
	}

	private class ThemedControl : TemplatedControl
	{
		#region Properties

		public Visual VisualChild => VisualChildren?.SingleOrDefault();

		#endregion
	}

	private class ThemedControl2 : TemplatedControl
	{
		#region Properties

		public Visual VisualChild => VisualChildren?.SingleOrDefault();

		#endregion
	}

	#endregion
}