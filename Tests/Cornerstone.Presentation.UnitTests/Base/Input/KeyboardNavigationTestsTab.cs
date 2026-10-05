#region References

using System.Collections.Generic;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Base.Utilities;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class KeyboardNavigationTestsTab
{
	#region Methods

	[PresentationTestMethod]
	public void CannotFocusChildOfDisabledControl()
	{
		Button start;
		Button expected;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				(start = new Button { Name = "Button1" }),
				new Border
				{
					IsEnabled = false,
					Child = new Button { Name = "Button2" }
				},
				(expected = new Button { Name = "Button3" })
			}
		};

		var current = (IInputElement) start;
		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.Same(expected, result);
	}

	[PresentationTestMethod]
	public void FocusesFirstChildFromNoFocus()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var button = new Button();
		var root = new TestRoot(button);
		var target = new KeyboardNavigationHandler();

		target.SetOwner(root);

		root.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Tab
		});

		CornerstoneTest.IsTrue(button.IsFocused);
	}

	[PresentationTestMethod]
	public void NextContainedReturnsNextControlInContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
					Children =
					{
						new Button { Name = "Button1" },
						(current = new Button { Name = "Button2" }),
						(next = new Button { Name = "Button3" })
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextContainedStopsAtEnd()
	{
		Button current;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
					Children =
					{
						new Button { Name = "Button1" },
						new Button { Name = "Button2" },
						(current = new Button { Name = "Button3" })
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.IsNull(result);
	}

	[PresentationTestMethod]
	public void NextContinueDoesntEnterPanelWithTabNavigationNone()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						(next = new Button { Name = "Button1" }),
						new Button { Name = "Button2" },
						(current = new Button { Name = "Button3" })
					}
				},
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.None,
					Children =
					{
						new StackPanel
						{
							Children =
							{
								new Button { Name = "Button4" },
								new Button { Name = "Button5" },
								new Button { Name = "Button6" }
							}
						}
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextContinueReturnsChildOfTopLevel()
	{
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				(next = new Button { Name = "Button1" })
			}
		};

		var result = KeyboardNavigationHandler.GetNext(top, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextContinueReturnsFirstControlInNextSiblingContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button1" },
						new Button { Name = "Button2" },
						(current = new Button { Name = "Button3" })
					}
				},
				new StackPanel
				{
					Children =
					{
						(next = new Button { Name = "Button4" }),
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextContinueReturnsFirstControlInNextUncleContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new StackPanel
						{
							Children =
							{
								new Button { Name = "Button1" },
								new Button { Name = "Button2" },
								(current = new Button { Name = "Button3" })
							}
						}
					}
				},
				new StackPanel
				{
					Children =
					{
						(next = new Button { Name = "Button4" }),
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextContinueReturnsNextControlInContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button1" },
						(current = new Button { Name = "Button2" }),
						(next = new Button { Name = "Button3" })
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextContinueReturnsNextSibling()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button1" },
						new Button { Name = "Button2" },
						(current = new Button { Name = "Button3" })
					}
				},
				(next = new Button { Name = "Button4" })
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextContinueWraps()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new StackPanel
						{
							Children =
							{
								(next = new Button { Name = "Button1" }),
								new Button { Name = "Button2" },
								new Button { Name = "Button3" }
							}
						}
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						(current = new Button { Name = "Button6" })
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextCycleReturnsNextControlInContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
					Children =
					{
						new Button { Name = "Button1" },
						(current = new Button { Name = "Button2" }),
						(next = new Button { Name = "Button3" })
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextCycleWrapsToFirst()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
					Children =
					{
						(next = new Button { Name = "Button1" }),
						new Button { Name = "Button2" },
						(current = new Button { Name = "Button3" })
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextNoneMovesToNextContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.None,
					Children =
					{
						new Button { Name = "Button1" },
						(current = new Button { Name = "Button2" }),
						new Button { Name = "Button3" }
					}
				},
				new StackPanel
				{
					Children =
					{
						(next = new Button { Name = "Button4" }),
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextNoneSkipsContainer()
	{
		StackPanel container;
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				(container = new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.None,
					Children =
					{
						new Button { Name = "Button1" },
						new Button { Name = "Button2" },
						new Button { Name = "Button3" }
					}
				}),
				new StackPanel
				{
					Children =
					{
						(next = new Button { Name = "Button4" }),
						new Button { Name = "Button5" },
						(current = new Button { Name = "Button6" })
					}
				}
			}
		};

		KeyboardNavigation.SetTabOnceActiveElement(container, next);

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextOnceMovesToActiveElement()
	{
		StackPanel container;
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				(container = new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
					Children =
					{
						new Button { Name = "Button1" },
						(next = new Button { Name = "Button2" }),
						new Button { Name = "Button3" }
					}
				}),
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						(current = new Button { Name = "Button6" })
					}
				}
			}
		};

		KeyboardNavigation.SetTabOnceActiveElement(container, next);

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextOnceMovesToNextContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
					Children =
					{
						new Button { Name = "Button1" },
						(current = new Button { Name = "Button2" }),
						new Button { Name = "Button3" }
					}
				},
				new StackPanel
				{
					Children =
					{
						(next = new Button { Name = "Button4" }),
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextSkipButtonWhenCommandCanExecuteIsFalse()
	{
		Button current;
		Button expected;
		var executed = false;

		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						(current = new Button { Name = "Button1" }),
						new Button
						{
							Name = "Button2",
							Command = new DelegateCommand(() => executed = true,
								_ => false)
						},
						(expected = new Button { Name = "Button3" })
					}
				}
			}
		};

		var testRoot = new TestRoot(top);

		top.ApplyTemplate();

		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next) as Button;

		CornerstoneTest.AreEqual(expected.Name, result?.Name);
		CornerstoneTest.IsFalse(executed);
	}

	[PresentationTestMethod]
	public void NextSkipsNonTabStopSiblings()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button1" },
						new Button { Name = "Button2" },
						(current = new Button { Name = "Button3" }),
						new Button { Name = "Button4", [KeyboardNavigation.IsTabStopProperty] = false }
					}
				},
				(next = new Button { Name = "Button5" })
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void NextSkipsUnfocusableSiblings()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button1" },
						new Button { Name = "Button2" },
						new StackPanel
						{
							Children =
							{
								(current = new Button { Name = "Button3" })
							}
						},
						new TextBlock { Name = "TextBlock" },
						(next = new Button { Name = "Button4" })
					}
				},
				new Button { Name = "Button5" },
				new Button { Name = "Button6" }
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void PreviousContainedDoesntSelectChildControl()
	{
		Decorator current;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
			Children =
			{
				(current = new Decorator
				{
					Focusable = true,
					Child = new Button()
				})
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.IsNull(result);
	}

	[PresentationTestMethod]
	public void PreviousContainedReturnsPreviousControlInContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
					Children =
					{
						(next = new Button { Name = "Button1" }),
						(current = new Button { Name = "Button2" }),
						new Button { Name = "Button3" }
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void PreviousContainedStopsAtBeginning()
	{
		Button current;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Contained,
					Children =
					{
						(current = new Button { Name = "Button1" }),
						new Button { Name = "Button2" },
						new Button { Name = "Button3" }
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.IsNull(result);
	}

	[PresentationTestMethod]
	public void PreviousContinueReturnsLastChildOfSibling()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button1" },
						new Button { Name = "Button2" },
						(next = new Button { Name = "Button3" })
					}
				},
				(current = new Button { Name = "Button4" })
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void PreviousContinueReturnsLastControlInPreviousNephewContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new StackPanel
						{
							Children =
							{
								new Button { Name = "Button1" },
								new Button { Name = "Button2" },
								(next = new Button { Name = "Button3" })
							}
						}
					}
				},
				new StackPanel
				{
					Children =
					{
						(current = new Button { Name = "Button4" }),
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void PreviousContinueReturnsLastControlInPreviousSiblingContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button1" },
						new Button { Name = "Button2" },
						(next = new Button { Name = "Button3" })
					}
				},
				new StackPanel
				{
					Children =
					{
						(current = new Button { Name = "Button4" }),
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void PreviousContinueReturnsParent()
	{
		Button current;

		var top = new Decorator
		{
			Focusable = true,
			Child = current = new Button
			{
				Name = "Button"
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(top, result);
	}

	[PresentationTestMethod]
	public void PreviousContinueReturnsPreviousControlInContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button1" },
						(next = new Button { Name = "Button2" }),
						(current = new Button { Name = "Button3" })
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void PreviousContinueWraps()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new StackPanel
						{
							Children =
							{
								(current = new Button { Name = "Button1" }),
								new Button { Name = "Button2" },
								new Button { Name = "Button3" }
							}
						}
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						(next = new Button { Name = "Button6" })
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void PreviousCycleReturnsPreviousControlInContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
					Children =
					{
						(next = new Button { Name = "Button1" }),
						(current = new Button { Name = "Button2" }),
						new Button { Name = "Button3" }
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void PreviousCycleWrapsToLast()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
					Children =
					{
						(current = new Button { Name = "Button1" }),
						new Button { Name = "Button2" },
						(next = new Button { Name = "Button3" })
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4" },
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void PreviousOnceMovesToActiveElement()
	{
		StackPanel container;
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				(container = new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
					Children =
					{
						new Button { Name = "Button1" },
						(next = new Button { Name = "Button2" }),
						new Button { Name = "Button3" }
					}
				}),
				new StackPanel
				{
					Children =
					{
						(current = new Button { Name = "Button4" }),
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		KeyboardNavigation.SetTabOnceActiveElement(container, next);

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void PreviousOnceMovesToFirstElement()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
					Children =
					{
						(next = new Button { Name = "Button1" }),
						new Button { Name = "Button2" },
						new Button { Name = "Button3" }
					}
				},
				new StackPanel
				{
					Children =
					{
						(current = new Button { Name = "Button4" }),
						new Button { Name = "Button5" },
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void PreviousOnceMovesToPreviousContainer()
	{
		Button current;
		Button next;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button1" },
						new Button { Name = "Button2" },
						(next = new Button { Name = "Button3" })
					}
				},
				new StackPanel
				{
					[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Once,
					Children =
					{
						new Button { Name = "Button4" },
						(current = new Button { Name = "Button5" }),
						new Button { Name = "Button6" }
					}
				}
			}
		};

		var result = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);

		CornerstoneTest.AreEqual(next, result);
	}

	[PresentationTestMethod]
	public void RespectsTabIndexMovingBackwards()
	{
		Button start;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button1", TabIndex = 5 },
						(start = new Button { Name = "Button2", TabIndex = 2 }),
						new Button { Name = "Button3", TabIndex = 1 }
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4", TabIndex = 3 },
						new Button { Name = "Button5", TabIndex = 6 },
						new Button { Name = "Button6", TabIndex = 4 }
					}
				}
			}
		};

		var result = new List<string>();
		var current = (IInputElement) start;

		do
		{
			result.Add(((Control) current).Name);
			current = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Previous);
		} while (current is not null && (current != start));

		CornerstoneTest.AreEqual(new[]
		{
			"Button2", "Button3", "Button5", "Button1", "Button6", "Button4"
		}, result);
	}

	[PresentationTestMethod]
	public void RespectsTabIndexMovingForwards()
	{
		Button start;

		var top = new StackPanel
		{
			[KeyboardNavigation.TabNavigationProperty] = KeyboardNavigationMode.Cycle,
			Children =
			{
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button1", TabIndex = 5 },
						(start = new Button { Name = "Button2", TabIndex = 2 }),
						new Button { Name = "Button3", TabIndex = 1 }
					}
				},
				new StackPanel
				{
					Children =
					{
						new Button { Name = "Button4", TabIndex = 3 },
						new Button { Name = "Button5", TabIndex = 6 },
						new Button { Name = "Button6", TabIndex = 4 }
					}
				}
			}
		};

		var result = new List<string>();
		var current = (IInputElement) start;

		do
		{
			result.Add(((Control) current).Name);
			current = KeyboardNavigationHandler.GetNext(current, NavigationDirection.Next);
		} while (current is object && (current != start));

		CornerstoneTest.AreEqual(new[]
		{
			"Button2", "Button4", "Button6", "Button1", "Button5", "Button3"
		}, result);
	}

	#endregion
}