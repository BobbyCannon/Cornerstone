#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Diagnostics;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Leak;

[TestClass]
public class ControlTests : ScopedTestBase
{
	#region Methods

	[ReleaseTestMethod]
	public void AttachedContextMenuIsFreed()
	{
		using (Start())
		{
			(WeakReference, WeakReference, WeakReference) AttachShowAndDetachContextMenu(Control control)
			{
				var menuItem1 = new MenuItem { Header = "Foo" };
				var menuItem2 = new MenuItem { Header = "Foo" };
				var contextMenu = new ContextMenu
				{
					Items =
					{
						menuItem1,
						menuItem2
					}
				};

				control.ContextMenu = contextMenu;
				contextMenu.Open(control);
				contextMenu.Close();
				control.ContextMenu = null;

				return (new WeakReference(menuItem1), new WeakReference(menuItem2), new WeakReference(contextMenu));
			}

			var window = new Window { Focusable = true };
			window.Show();

			CornerstoneTest.Same(window, window.FocusManager!.GetFocusedElement());

			var (weakMenuItem1, weakMenuItem2, weakContextMenu) = AttachShowAndDetachContextMenu(window);
			CornerstoneTest.IsTrue(weakMenuItem1.IsAlive);
			CornerstoneTest.IsTrue(weakMenuItem2.IsAlive);
			CornerstoneTest.IsTrue(weakContextMenu.IsAlive);

			((StubWindowImpl) window.PlatformImpl).ReleaseCallbacks();
			CollectGarbage();

			CornerstoneTest.IsFalse(weakMenuItem1.IsAlive);
			CornerstoneTest.IsFalse(weakMenuItem2.IsAlive);
			CornerstoneTest.IsFalse(weakContextMenu.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void AttachedControlFromContextMenuIsFreed()
	{
		using (Start())
		{
			var contextMenu = new ContextMenu();

			WeakReference Run()
			{
				var textBlock = new TextBlock
				{
					ContextMenu = contextMenu
				};
				var window = new Window
				{
					Content = textBlock
				};

				window.Show();

				// Do a layout and make sure that TextBlock gets added to visual tree.
				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.IsType<TextBlock>(window.Presenter!.Child);

				// Clear the content and ensure the TextBlock is removed.
				window.Content = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return new WeakReference(textBlock);
			}

			var weakTextBlock = Run();
			CornerstoneTest.IsTrue(weakTextBlock.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakTextBlock.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void CanvasIsFreed()
	{
		using (Start())
		{
			static WeakReference Run()
			{
				var canvas = new Canvas();
				var window = new Window
				{
					Content = canvas
				};

				window.Show();

				// Do a layout and make sure that Canvas gets added to visual tree.
				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.IsType<Canvas>(window.Presenter!.Child);

				// Clear the content and ensure the Canvas is removed.
				window.Content = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return new WeakReference(canvas);
			}

			var weakCanvas = Run();
			CornerstoneTest.IsTrue(weakCanvas.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakCanvas.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void ControlWithStyleRenderTransformIsFreed()
	{
		// # Issue #3545
		using (Start())
		{
			static WeakReference Run()
			{
				var window = new Window
				{
					Styles =
					{
						new Style(x => x.OfType<Canvas>())
						{
							Setters =
							{
								new Setter
								{
									Property = Visual.RenderTransformProperty,
									Value = new RotateTransform(45)
								}
							}
						}
					},
					Content = new Canvas()
				};

				window.Show();

				// Do a layout and make sure that Canvas gets added to visual tree with
				// its render transform.
				window.LayoutManager.ExecuteInitialLayoutPass();
				var canvas = CornerstoneTest.IsType<Canvas>(window.Presenter!.Child);
				CornerstoneTest.IsType<RotateTransform>(canvas.RenderTransform);

				// Clear the content and ensure the Canvas is removed.
				window.Content = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return new WeakReference(canvas);
			}

			var weakCanvas = Run();
			CornerstoneTest.IsTrue(weakCanvas.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakCanvas.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void ElementNameBindingInDataTemplateIsFreed()
	{
		using (Start())
		{
			// Canvas 10 + ListBoxItem Padding 8,2 + BorderThickness 0,1.
			const double ListBoxItemHeight = 16;
			const double TextBoxHeight = 25;

			var items = new ObservableCollection<int>(Enumerable.Range(0, 10));
			NameScope ns;
			TextBox tb;
			ListBox lb;

			var weakCanvases = new List<WeakReference>();

			var window = new Window
			{
				[NameScope.NameScopeProperty] = ns = new NameScope(),
				Width = 100,
				Height = (items.Count * ListBoxItemHeight) + TextBoxHeight,
				Content = new DockPanel
				{
					Children =
					{
						(tb = new TextBox
						{
							Name = "tb",
							Text = "foo",
							Height = TextBoxHeight,
							[DockPanel.DockProperty] = Dock.Top
						}),
						(lb = new ListBox
						{
							ItemsSource = items,
							ItemTemplate = new FuncDataTemplate<int>((_, _) =>
							{
								var canvas = new Canvas
								{
									Width = 10,
									Height = 10,
									[!Control.TagProperty] = new Binding
									{
										ElementName = "tb",
										Path = "Text",
										NameScope = new WeakReference<INameScope>(ns)
									}
								};
								weakCanvases.Add(new WeakReference(canvas));
								return canvas;
							}),
							Padding = new Thickness(0)
						})
					}
				}
			};

			tb.RegisterInNameScope(ns);

			window.Show();
			window.LayoutManager.ExecuteInitialLayoutPass();

			void AssertInitialItemState()
			{
				var item0 = (ListBoxItem) lb.GetRealizedContainers().First();
				var canvas0 = (Canvas) item0.Presenter!.Child!;
				CornerstoneTest.AreEqual("foo", canvas0.Tag);
			}

			CornerstoneTest.AreEqual(10, lb.GetRealizedContainers().Count());
			AssertInitialItemState();

			items.Clear();
			window.LayoutManager.ExecuteLayoutPass();

			CornerstoneTest.Empty(lb.GetRealizedContainers());
			CornerstoneTest.AreEqual(10, weakCanvases.Count);

			foreach (var weakReference in weakCanvases)
			{
				CornerstoneTest.IsTrue(weakReference.IsAlive);
			}

			CollectGarbage();

			foreach (var weakReference in weakCanvases)
			{
				CornerstoneTest.IsFalse(weakReference.IsAlive);
			}
		}
	}

	[ReleaseTestMethod]
	public void FlyoutIsFreed()
	{
		using (Start())
		{
			static (WeakReference, WeakReference) Run()
			{
				var window = new Window();
				var source = new Button
				{
					Template = new FuncControlTemplate<Button>((_, _) =>
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

				var templateChild = (Button) source.GetVisualChildren().Single();
				var flyout = CornerstoneTest.IsType<Flyout>(templateChild.Flyout);
				var textBlock = CornerstoneTest.IsType<TextBlock>(flyout.Content);

				flyout.ShowAt(templateChild);

				flyout.Hide();

				// Detach the button from the logical tree, so there is no reference to it
				window.Content = null;

				// Mock keep reference on a Popup via InvocationsCollection. So let's clear it before.
				((StubWindowImpl) window.PlatformImpl).Calls.Clear();

				return (new WeakReference(flyout), new WeakReference(textBlock));
			}

			var (weakFlyout, weakTextBlock) = Run();
			CornerstoneTest.IsTrue(weakFlyout.IsAlive);
			CornerstoneTest.IsTrue(weakTextBlock.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakFlyout.IsAlive);
			CornerstoneTest.IsFalse(weakTextBlock.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void HotKeyManagerShouldReleaseReferenceWhenControlDetached()
	{
		using (Start())
		{
			static WeakReference Run()
			{
				var gesture1 = new KeyGesture(Key.A, KeyModifiers.Control);
				var tl = new Window
				{
					Content = new ItemsControl()
				};

				tl.Show();

				var button = new Button();
				tl.Content = button;
				tl.Template = CreateWindowTemplate();
				tl.ApplyTemplate();
				tl.Presenter!.ApplyTemplate();
				HotKeyManager.SetHotKey(button, gesture1);

				// Detach the button from the logical tree, so there is no reference to it
				tl.Content = null;
				tl.ApplyTemplate();

				return new WeakReference(button);
			}

			var weakButton = Run();
			CornerstoneTest.IsTrue(weakButton.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakButton.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void HotKeyManagerShouldReleaseReferenceWhenControlInItemTemplateDetached()
	{
		using (Start())
		{
			static List<WeakReference> Run()
			{
				var gesture1 = new KeyGesture(Key.A, KeyModifiers.Control);

				var tl = new Window { SizeToContent = SizeToContent.WidthAndHeight, IsVisible = true };
				var lm = tl.LayoutManager;
				tl.Show();

				var keyGestures = new OldPresentationList<KeyGesture> { gesture1 };
				var weakButtons = new List<WeakReference>();
				var listBox = new ListBox
				{
					Width = 100,
					Height = 100,

					// Create a button with binding to the KeyGesture in the template and add it to references list
					ItemTemplate = new FuncDataTemplate(typeof(KeyGesture), (o, _) =>
					{
						var keyGesture = o as KeyGesture;
						var button = new Button
						{
							DataContext = keyGesture,
							[!Button.HotKeyProperty] = new Binding("")
						};
						weakButtons.Add(new WeakReference(button));
						return button;
					})
				};

				// Add the listbox and render it
				tl.Content = listBox;
				lm.ExecuteInitialLayoutPass();
				listBox.ItemsSource = keyGestures;
				lm.ExecuteLayoutPass();

				// Let the button detach when clearing the source items
				keyGestures.Clear();
				lm.ExecuteLayoutPass();

				// Add it again to double check,and render
				keyGestures.Add(gesture1);
				lm.ExecuteLayoutPass();

				keyGestures.Clear();
				lm.ExecuteLayoutPass();

				return weakButtons;
			}

			var weakButtons = Run();

			CornerstoneTest.NotEmpty(weakButtons);

			foreach (var weakReference in weakButtons)
			{
				CornerstoneTest.IsTrue(weakReference.IsAlive);
			}

			CollectGarbage();

			foreach (var weakReference in weakButtons)
			{
				CornerstoneTest.IsFalse(weakReference.IsAlive);
			}
		}
	}

	[ReleaseTestMethod]
	public void LayoutTransformControlIsFreed()
	{
		using (Start())
		{
			var transform = new RotateTransform { Angle = 90 };

			(WeakReference, WeakReference) Run()
			{
				var canvas = new Canvas();
				var layoutTransformControl = new LayoutTransformControl
				{
					LayoutTransform = transform,
					Child = canvas
				};
				var window = new Window
				{
					Content = layoutTransformControl
				};

				window.Show();

				// Do a layout and make sure that LayoutTransformControl gets added to visual tree
				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.IsType<LayoutTransformControl>(window.Presenter!.Child);
				CornerstoneTest.NotEmpty(window.Presenter.Child.GetVisualChildren());

				// Clear the content and ensure the LayoutTransformControl is removed.
				window.Content = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return (new WeakReference(layoutTransformControl), new WeakReference(canvas));
			}

			var (weakLayoutTransformControl, weakCanvas) = Run();
			CornerstoneTest.IsTrue(weakLayoutTransformControl.IsAlive);
			CornerstoneTest.IsTrue(weakCanvas.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakLayoutTransformControl.IsAlive);
			CornerstoneTest.IsFalse(weakCanvas.IsAlive);

			// We are keeping transform alive to simulate a resource that outlives the control.
			GC.KeepAlive(transform);
		}
	}

	[ReleaseTestMethod]
	public void MenuIsFreed()
	{
		using (Start())
		{
			var window = new Window();

			WeakReference Run()
			{
				var menu = new Menu();
				window.Content = menu;

				window.Show();

				// Do a layout and make sure that Menu gets added to visual tree
				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.IsType<Menu>(window.Presenter!.Child);
				CornerstoneTest.NotEmpty(window.Presenter.Child.GetVisualChildren());

				// Clear the content and ensure the Menu is removed.
				window.Content = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return new WeakReference(menu);
			}

			var weakMenu = Run();
			CornerstoneTest.IsTrue(weakMenu.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakMenu.IsAlive);

			GC.KeepAlive(window);
		}
	}

	[ReleaseTestMethod]
	public void NamedCanvasIsFreed()
	{
		using (Start())
		{
			static WeakReference Run()
			{
				var scope = new NameScope();
				var canvas = new Canvas { Name = "foo" };
				var window = new Window
				{
					Content = canvas.RegisterInNameScope(scope)
				};
				NameScope.SetNameScope(window, scope);

				window.Show();

				// Do a layout and make sure that Canvas gets added to visual tree.
				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.IsType<Canvas>(window.Find<Canvas>("foo"));
				CornerstoneTest.IsType<Canvas>(window.Presenter!.Child);

				// Clear the content and ensure the Canvas is removed.
				window.Content = null;
				NameScope.SetNameScope(window, null);

				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return new WeakReference(canvas);
			}

			var weakCanvas = Run();
			CornerstoneTest.IsTrue(weakCanvas.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakCanvas.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void PathIsFreed()
	{
		using (Start())
		{
			var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };

			WeakReference Run()
			{
				var path = new Path
				{
					Data = geometry
				};
				var window = new Window
				{
					Content = path
				};

				window.Show();

				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.IsType<Path>(window.Presenter!.Child);

				window.Content = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return new WeakReference(path);
			}

			var weakPath = Run();
			CornerstoneTest.IsTrue(weakPath.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakPath.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void PolylineWithObservableCollectionPointsBindingIsFreed()
	{
		using (Start())
		{
			var observableCollection = new ObservableCollection<Point> { new() };

			WeakReference Run()
			{
				var polyline = new Polyline
				{
					Points = observableCollection
				};
				var window = new Window
				{
					Content = polyline
				};

				window.Show();

				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.IsType<Polyline>(window.Presenter!.Child);

				window.Content = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return new WeakReference(polyline);
			}

			var weakPolyline = Run();
			CornerstoneTest.IsTrue(weakPolyline.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakPolyline.IsAlive);

			// We are keeping collection alive to simulate a resource that outlives the control.
			GC.KeepAlive(observableCollection);
		}
	}

	[PresentationTestMethod]
	public void RendererIsDisposed()
	{
		using (Start())
		{
			var screen1 = new MockScreen(1.75, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 966)), true);
			var screens = new StubScreenImpl(screen1);
			var impl = new StubWindowImpl();
			impl.RaiseClosedOnDispose = true;
			impl.Screens = screens;

			PresentationLocator.CurrentMutable.Bind<IWindowingPlatform>()
				.ToConstant(new MockWindowingPlatform(() => impl));
			var window = new Window
			{
				Content = new Button()
			};
			window.Show();
			window.Close();
			CornerstoneTest.IsTrue(window.Renderer.IsDisposed);
		}
	}

	[ReleaseTestMethod]
	public void ScrollViewerWithContentIsFreed()
	{
		using (Start())
		{
			static WeakReference Run()
			{
				var canvas = new Canvas();
				var window = new Window
				{
					Content = new ScrollViewer
					{
						Content = canvas
					}
				};

				window.Show();

				// Do a layout and make sure that ScrollViewer gets added to visual tree and its
				// template applied.
				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.IsType<ScrollViewer>(window.Presenter!.Child);
				CornerstoneTest.IsType<Canvas>(((ScrollViewer) window.Presenter!.Child).Presenter!.Child);

				// Clear the content and ensure the ScrollViewer is removed.
				window.Content = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return new WeakReference(canvas);
			}

			var weakCanvas = Run();
			CornerstoneTest.IsTrue(weakCanvas.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakCanvas.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void SliderIsFreed()
	{
		using (Start())
		{
			static WeakReference Run()
			{
				var slider = new Slider();
				var window = new Window
				{
					Content = slider
				};

				window.Show();

				// Do a layout and make sure that Slider gets added to visual tree.
				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.IsType<Slider>(window.Presenter!.Child);

				// Clear the content and ensure the Slider is removed.
				window.Content = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return new WeakReference(slider);
			}

			var weakSlider = Run();
			CornerstoneTest.IsTrue(weakSlider.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakSlider.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void StandaloneContextMenuIsFreed()
	{
		using (Start())
		{
			(WeakReference, WeakReference, WeakReference) BuildAndShowContextMenu(Control control)
			{
				var menuItem1 = new MenuItem { Header = "Foo" };
				var menuItem2 = new MenuItem { Header = "Foo" };
				var contextMenu = new ContextMenu
				{
					Items =
					{
						menuItem1,
						menuItem2
					}
				};

				contextMenu.Open(control);
				contextMenu.Close();

				return (new WeakReference(menuItem1), new WeakReference(menuItem2), new WeakReference(contextMenu));
			}

			var window = new Window { Focusable = true };
			window.Show();

			CornerstoneTest.Same(window, window.FocusManager!.GetFocusedElement());

			var (weakMenuItem1, weakMenuItem2, weakContextMenu1) = BuildAndShowContextMenu(window);
			var (weakMenuItem3, weakMenuItem4, weakContextMenu2) = BuildAndShowContextMenu(window);

			CornerstoneTest.IsTrue(weakMenuItem1.IsAlive);
			CornerstoneTest.IsTrue(weakMenuItem2.IsAlive);
			CornerstoneTest.IsTrue(weakContextMenu1.IsAlive);
			CornerstoneTest.IsTrue(weakMenuItem3.IsAlive);
			CornerstoneTest.IsTrue(weakMenuItem4.IsAlive);
			CornerstoneTest.IsTrue(weakContextMenu2.IsAlive);

			((StubWindowImpl) window.PlatformImpl).ReleaseCallbacks();
			CollectGarbage();

			CornerstoneTest.IsFalse(weakMenuItem1.IsAlive);
			CornerstoneTest.IsFalse(weakMenuItem2.IsAlive);
			CornerstoneTest.IsFalse(weakContextMenu1.IsAlive);
			CornerstoneTest.IsFalse(weakMenuItem3.IsAlive);
			CornerstoneTest.IsFalse(weakMenuItem4.IsAlive);
			CornerstoneTest.IsFalse(weakContextMenu2.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void TabItemIsFreed()
	{
		using (Start())
		{
			static WeakReference Run()
			{
				var tabItem = new TabItem();
				var window = new Window
				{
					Content = new TabControl
					{
						ItemsSource = new[] { tabItem }
					}
				};

				window.Show();

				// Do a layout and make sure that TabControl and TabItem gets added to visual tree.
				window.LayoutManager.ExecuteInitialLayoutPass();
				var tabControl = CornerstoneTest.IsType<TabControl>(window.Presenter!.Child);
				CornerstoneTest.IsType<TabItem>(tabControl.Presenter!.Panel!.Children[0]);

				// Clear the items and ensure the TabItem is removed.
				tabControl.ItemsSource = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.Empty(tabControl.Presenter.Panel.Children);

				return new WeakReference(tabItem);
			}

			var weakTabItem = Run();
			CornerstoneTest.IsTrue(weakTabItem.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakTabItem.IsAlive);
		}
	}

	[PresentationTestMethod]
	public void TextBoxClassListenersAreFreed()
	{
		using (Start())
		{
			TextBox textBox;

			var window = new Window
			{
				Content = textBox = new TextBox()
			};

			window.Show();

			// Do a layout and make sure that TextBox gets added to visual tree and its 
			// template applied.
			window.LayoutManager.ExecuteInitialLayoutPass();
			CornerstoneTest.Same(textBox, window.Presenter!.Child);

			// Get the border from the TextBox template.
			var border = textBox.GetTemplateDescendants().FirstOrDefault(x => x.Name == "border");

			// The TextBox should have subscriptions to its Classes collection from the
			// default theme.
			CornerstoneTest.AreNotEqual(0, textBox.Classes.ListenerCount);

			// Clear the content and ensure the TextBox is removed.
			window.Content = null;
			window.LayoutManager.ExecuteLayoutPass();
			CornerstoneTest.IsNull(window.Presenter.Child);

			// Check that the TextBox has no subscriptions to its Classes collection.
			CornerstoneTest.IsNull(((INotifyCollectionChangedDebug) textBox.Classes).GetCollectionChangedSubscribers());
		}
	}

	[ReleaseTestMethod]
	public void TextBoxIsFreed()
	{
		using (Start())
		{
			static WeakReference Run()
			{
				var textBox = new TextBox();
				var window = new Window
				{
					Content = textBox
				};

				window.Show();

				// Do a layout and make sure that TextBox gets added to visual tree and its 
				// template applied.
				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.IsType<TextBox>(window.Presenter!.Child);
				CornerstoneTest.NotEmpty(window.Presenter.Child.GetVisualChildren());

				// Clear the content and ensure the TextBox is removed.
				window.Content = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return new WeakReference(textBox);
			}

			var weakTextBox = Run();
			CornerstoneTest.IsTrue(weakTextBox.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakTextBox.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void TextBoxWithXamlBindingIsFreed()
	{
		using (Start())
		{
			static (WeakReference, WeakReference) Run()
			{
				var node = new Node { Name = "foo" };
				var window = new Window
				{
					DataContext = node,
					Content = new TextBox()
				};

				var binding = new Binding
				{
					Path = "Name"
				};

				var textBox = (TextBox) window.Content;
				textBox.Bind(TextBox.TextProperty, binding);

				window.Show();

				// Do a layout and make sure that TextBox gets added to visual tree and its
				// Text property set.
				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.IsType<TextBox>(window.Presenter!.Child);
				CornerstoneTest.AreEqual("foo", ((TextBox) window.Presenter.Child).Text);

				// Clear the content and DataContext and ensure the TextBox is removed.
				window.Content = null;
				window.DataContext = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter.Child);

				return (new WeakReference(node), new WeakReference(textBox));
			}

			var (weakNode, weakTextBox) = Run();
			CornerstoneTest.IsTrue(weakNode.IsAlive);
			CornerstoneTest.IsTrue(weakTextBox.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakNode.IsAlive);
			CornerstoneTest.IsFalse(weakTextBox.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void ToolTipIsFreed()
	{
		using (Start())
		{
			static (WeakReference, WeakReference) Run()
			{
				var window = new Window();
				TextBlock textBlock = null;
				var source = new Button
				{
					Template = new FuncControlTemplate<Button>((_, _) =>
					{
						CornerstoneTest.IsNull(textBlock);
						textBlock = new TextBlock
						{
							[~TextBlock.TextProperty] =
								new TemplateBinding(ContentControl.ContentProperty)
						};
						return new Decorator
						{
							[ToolTip.TipProperty] = textBlock
						};
					})
				};

				window.Content = source;
				window.Show();

				var templateChild = (Decorator) source.GetVisualChildren().Single();
				ToolTip.SetIsOpen(templateChild, true);
				var toolTip = templateChild.GetValue(ToolTip.ToolTipProperty);
				CornerstoneTest.IsNotNull(toolTip);

				ToolTip.SetIsOpen(templateChild, false);

				// Detach the button from the logical tree, so there is no reference to it
				window.Content = null;

				// Mock keep reference on a Popup via InvocationsCollection. So let's clear it before.
				((StubWindowImpl) window.PlatformImpl).Calls.Clear();

				return (new WeakReference(toolTip), new WeakReference(textBlock));
			}

			var (weakTooltip, weakTextBlock) = Run();
			CornerstoneTest.IsTrue(weakTooltip.IsAlive);
			CornerstoneTest.IsTrue(weakTextBlock.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakTooltip.IsAlive);
			CornerstoneTest.IsFalse(weakTextBlock.IsAlive);
		}
	}

	[ReleaseTestMethod]
	public void TreeViewIsFreed()
	{
		using (Start())
		{
			static WeakReference Run()
			{
				var nodes = new[]
				{
					new Node
					{
						Children = new[] { new Node() }
					}
				};

				TreeView target;

				var window = new Window
				{
					Content = target = new TreeView
					{
						DataTemplates =
						{
							new FuncTreeDataTemplate<Node>(
								(x, _) => new TextBlock { Text = x.Name },
								x => x.Children ?? [])
						},
						ItemsSource = nodes
					}
				};

				window.Show();

				// Do a layout and make sure that TreeViewItems get realized.
				window.LayoutManager.ExecuteInitialLayoutPass();
				CornerstoneTest.Single(target.GetRealizedContainers());

				// Clear the content and ensure the TreeView is removed.
				window.Content = null;
				window.LayoutManager.ExecuteLayoutPass();
				CornerstoneTest.IsNull(window.Presenter!.Child);

				return new WeakReference(target);
			}

			var weakTreeView = Run();
			CornerstoneTest.IsTrue(weakTreeView.IsAlive);

			CollectGarbage();

			CornerstoneTest.IsFalse(weakTreeView.IsAlive);
		}
	}

	private static void CollectGarbage()
	{
		// Process all Loaded events to free control reference(s)
		KeyboardDevice.Instance?.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
		GC.WaitForPendingFinalizers();
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

		Dispatcher.UIThread.RunJobs();
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
		GC.WaitForPendingFinalizers();
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
	}

	private static FuncControlTemplate CreateWindowTemplate()
	{
		return new FuncControlTemplate<Window>((parent, scope) =>
		{
			return new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty]
			}.RegisterInNameScope(scope);
		});
	}

	private IDisposable Start()
	{
		static void Cleanup()
		{
			// KeyboardDevice holds a reference to the focused item.
			KeyboardDevice.Instance?.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);

			// Empty the dispatcher queue.
			Dispatcher.UIThread.RunJobs();
		}

		return new CompositeDisposable
		{
			Disposable.Create(Cleanup),
			UnitTestApplication.Start(TestServices.StyledWindow.With(
				keyboardDevice: () => new KeyboardDevice(),
				inputManager: new InputManager(),
				accessKeyHandler: () => new AccessKeyHandler()))
		};
	}

	#endregion

	#region Classes

	private class Node
	{
		#region Properties

		public IEnumerable<Node> Children { get; set; }
		public string Name { get; set; }

		#endregion
	}

	#endregion
}