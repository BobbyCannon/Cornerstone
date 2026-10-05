#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.CommandBars;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class CommandBarButtonTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CommandDefaultIsNull()
	{
		CornerstoneTest.IsNull(new CommandBarButton().Command);
	}

	[PresentationTestMethod]
	public void CommandParameterDefaultIsNull()
	{
		CornerstoneTest.IsNull(new CommandBarButton().CommandParameter);
	}

	[PresentationTestMethod]
	public void CommandParameterRoundTrip()
	{
		var btn = new CommandBarButton { CommandParameter = "param" };
		CornerstoneTest.AreEqual("param", btn.CommandParameter);
	}

	[PresentationTestMethod]
	public void CommandRoundTrip()
	{
		var btn = new CommandBarButton();
		var cmd = new DelegateCommand(_ => { });
		btn.Command = cmd;
		CornerstoneTest.Same(cmd, btn.Command);
	}

	[PresentationTestMethod]
	public void DynamicOverflowOrderDefaultIsZero()
	{
		CornerstoneTest.AreEqual(0, new CommandBarButton().DynamicOverflowOrder);
	}

	[PresentationTestMethod]
	public void DynamicOverflowOrderRoundTrip()
	{
		var btn = new CommandBarButton { DynamicOverflowOrder = 3 };
		CornerstoneTest.AreEqual(3, btn.DynamicOverflowOrder);
	}

	[PresentationTestMethod]
	public void ForegroundDoesNotSetOrOverwriteIconElementForeground()
	{
		var icon = new PathIcon();
		var btn = new CommandBarButton
		{
			Icon = icon,
			Foreground = Brushes.Red
		};

		CornerstoneTest.IsFalse(icon.IsSet(TemplatedControl.ForegroundProperty));

		btn.Foreground = Brushes.Blue;

		CornerstoneTest.IsFalse(icon.IsSet(TemplatedControl.ForegroundProperty));

		icon.Foreground = Brushes.Green;
		btn.Foreground = Brushes.Red;

		CornerstoneTest.Same(Brushes.Green, icon.Foreground);
	}

	[PresentationTestMethod]
	public void ICommandBarElementIsCompactReadWrite()
	{
		ICommandBarElement elem = new CommandBarButton();
		elem.IsCompact = true;
		CornerstoneTest.IsTrue(elem.IsCompact);
	}

	[PresentationTestMethod]
	public void IconDefaultIsNull()
	{
		CornerstoneTest.IsNull(new CommandBarButton().Icon);
	}

	[PresentationTestMethod]
	public void IconRoundTrip()
	{
		var btn = new CommandBarButton();
		var icon = new object();
		btn.Icon = icon;
		CornerstoneTest.Same(icon, btn.Icon);
	}

	[PresentationTestMethod]
	public void ImplementsICommandBarElement()
	{
		CornerstoneTest.IsAssignableFrom<ICommandBarElement>(new CommandBarButton());
	}

	[PresentationTestMethod]
	public void IsCompactDefaultIsFalse()
	{
		CornerstoneTest.IsFalse(new CommandBarButton().IsCompact);
	}

	[PresentationTestMethod]
	public void IsCompactRoundTrip()
	{
		var btn = new CommandBarButton { IsCompact = true };
		CornerstoneTest.IsTrue(btn.IsCompact);
	}

	[PresentationTestMethod]
	public void IsInOverflowDefaultIsFalse()
	{
		CornerstoneTest.IsFalse(new CommandBarButton().IsInOverflow);
	}

	[PresentationTestMethod]
	public void IsInOverflowRoundTrip()
	{
		var btn = new CommandBarButton { IsInOverflow = true };
		CornerstoneTest.IsTrue(btn.IsInOverflow);
	}

	[PresentationTestMethod]
	public void LabelDefaultIsNull()
	{
		CornerstoneTest.IsNull(new CommandBarButton().Label);
	}

	[PresentationTestMethod]
	public void LabelPositionDefaultIsBottom()
	{
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Bottom, new CommandBarButton().LabelPosition);
	}

	[PresentationTestMethod]
	public void LabelPositionRoundTrip()
	{
		var btn = new CommandBarButton { LabelPosition = CommandBarDefaultLabelPosition.Right };
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Right, btn.LabelPosition);
	}

	[PresentationTestMethod]
	public void LabelRoundTrip()
	{
		var btn = new CommandBarButton { Label = "Save" };
		CornerstoneTest.AreEqual("Save", btn.Label);
	}

	#endregion
}

[TestClass]
public class CommandBarToggleButtonTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CommandDefaultIsNull()
	{
		CornerstoneTest.IsNull(new CommandBarToggleButton().Command);
	}

	[PresentationTestMethod]
	public void CommandParameterRoundTrip()
	{
		var btn = new CommandBarToggleButton { CommandParameter = 42 };
		CornerstoneTest.AreEqual(42, btn.CommandParameter);
	}

	[PresentationTestMethod]
	public void CommandRoundTrip()
	{
		var btn = new CommandBarToggleButton();
		var cmd = new DelegateCommand(_ => { });
		btn.Command = cmd;
		CornerstoneTest.Same(cmd, btn.Command);
	}

	[PresentationTestMethod]
	public void DynamicOverflowOrderDefaultIsZero()
	{
		CornerstoneTest.AreEqual(0, new CommandBarToggleButton().DynamicOverflowOrder);
	}

	[PresentationTestMethod]
	public void DynamicOverflowOrderRoundTrip()
	{
		var btn = new CommandBarToggleButton { DynamicOverflowOrder = 5 };
		CornerstoneTest.AreEqual(5, btn.DynamicOverflowOrder);
	}

	[PresentationTestMethod]
	public void ForegroundDoesNotSetOrOverwriteIconElementForeground()
	{
		var icon = new PathIcon();
		var btn = new CommandBarToggleButton
		{
			Icon = icon,
			Foreground = Brushes.Red
		};

		CornerstoneTest.IsFalse(icon.IsSet(TemplatedControl.ForegroundProperty));

		btn.Foreground = Brushes.Blue;

		CornerstoneTest.IsFalse(icon.IsSet(TemplatedControl.ForegroundProperty));

		icon.Foreground = Brushes.Green;
		btn.Foreground = Brushes.Red;

		CornerstoneTest.Same(Brushes.Green, icon.Foreground);
	}

	[PresentationTestMethod]
	public void ICommandBarElementIsCompactReadWrite()
	{
		ICommandBarElement elem = new CommandBarToggleButton();
		elem.IsCompact = true;
		CornerstoneTest.IsTrue(elem.IsCompact);
	}

	[PresentationTestMethod]
	public void IconDefaultIsNull()
	{
		CornerstoneTest.IsNull(new CommandBarToggleButton().Icon);
	}

	[PresentationTestMethod]
	public void ImplementsICommandBarElement()
	{
		CornerstoneTest.IsAssignableFrom<ICommandBarElement>(new CommandBarToggleButton());
	}

	[PresentationTestMethod]
	public void IsCompactDefaultIsFalse()
	{
		CornerstoneTest.IsFalse(new CommandBarToggleButton().IsCompact);
	}

	[PresentationTestMethod]
	public void IsCompactRoundTrip()
	{
		var btn = new CommandBarToggleButton { IsCompact = true };
		CornerstoneTest.IsTrue(btn.IsCompact);
	}

	[PresentationTestMethod]
	public void IsInOverflowDefaultIsFalse()
	{
		CornerstoneTest.IsFalse(new CommandBarToggleButton().IsInOverflow);
	}

	[PresentationTestMethod]
	public void LabelDefaultIsNull()
	{
		CornerstoneTest.IsNull(new CommandBarToggleButton().Label);
	}

	[PresentationTestMethod]
	public void LabelPositionDefaultIsBottom()
	{
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Bottom, new CommandBarToggleButton().LabelPosition);
	}

	[PresentationTestMethod]
	public void LabelPositionRoundTrip()
	{
		var btn = new CommandBarToggleButton { LabelPosition = CommandBarDefaultLabelPosition.Collapsed };
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Collapsed, btn.LabelPosition);
	}

	[PresentationTestMethod]
	public void LabelRoundTrip()
	{
		var btn = new CommandBarToggleButton { Label = "Bold" };
		CornerstoneTest.AreEqual("Bold", btn.Label);
	}

	#endregion
}

[TestClass]
public class CommandBarSeparatorTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void DerivesFromSeparator()
	{
		CornerstoneTest.IsAssignableFrom<Separator>(new CommandBarSeparator());
	}

	[PresentationTestMethod]
	public void ICommandBarElementIsCompactReadWrite()
	{
		ICommandBarElement elem = new CommandBarSeparator();
		elem.IsCompact = true;
		CornerstoneTest.IsTrue(elem.IsCompact);
	}

	[PresentationTestMethod]
	public void ImplementsICommandBarElement()
	{
		CornerstoneTest.IsAssignableFrom<ICommandBarElement>(new CommandBarSeparator());
	}

	[PresentationTestMethod]
	public void IsCompactDefaultIsFalse()
	{
		CornerstoneTest.IsFalse(new CommandBarSeparator().IsCompact);
	}

	[PresentationTestMethod]
	public void IsCompactRoundTrip()
	{
		var sep = new CommandBarSeparator { IsCompact = true };
		CornerstoneTest.IsTrue(sep.IsCompact);
	}

	[PresentationTestMethod]
	public void IsInOverflowDefaultIsFalse()
	{
		CornerstoneTest.IsFalse(new CommandBarSeparator().IsInOverflow);
	}

	[PresentationTestMethod]
	public void IsInOverflowRoundTrip()
	{
		var sep = new CommandBarSeparator { IsInOverflow = true };
		CornerstoneTest.IsTrue(sep.IsInOverflow);
	}

	#endregion
}

[TestClass]
public class CommandBarEnumTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void LabelPositionBottomIsZero()
	{
		CornerstoneTest.AreEqual(0, (int) CommandBarDefaultLabelPosition.Bottom);
	}

	[PresentationTestMethod]
	public void LabelPositionCollapsedIsTwo()
	{
		CornerstoneTest.AreEqual(2, (int) CommandBarDefaultLabelPosition.Collapsed);
	}

	[PresentationTestMethod]
	public void LabelPositionRightIsOne()
	{
		CornerstoneTest.AreEqual(1, (int) CommandBarDefaultLabelPosition.Right);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilityAutoIsZero()
	{
		CornerstoneTest.AreEqual(0, (int) CommandBarOverflowButtonVisibility.Auto);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilityCollapsedIsTwo()
	{
		CornerstoneTest.AreEqual(2, (int) CommandBarOverflowButtonVisibility.Collapsed);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilityVisibleIsOne()
	{
		CornerstoneTest.AreEqual(1, (int) CommandBarOverflowButtonVisibility.Visible);
	}

	#endregion
}

[TestClass]
public class CommandBarDefaultsTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ContentDefaultIsNull()
	{
		CornerstoneTest.IsNull(new CommandBar().Content);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionIsBottom()
	{
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Bottom, new CommandBar().DefaultLabelPosition);
	}

	[PresentationTestMethod]
	public void ForegroundIsInheritedByCommandBarButtonPathIconThroughThemeTemplate()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var icon = new PathIcon();
		var btn = new CommandBarButton
		{
			Icon = icon
		};
		var commandBar = new CommandBar
		{
			Foreground = Brushes.Red
		};
		commandBar.PrimaryCommands.Add(btn);

		CornerstoneTest.Same(icon, ApplyCornerstoneThemeAndGetPresentedPathIcon(commandBar, btn));
		CornerstoneTest.Same(Brushes.Red, icon.Foreground);

		commandBar.Foreground = Brushes.Blue;

		CornerstoneTest.Same(Brushes.Blue, icon.Foreground);
	}

	[PresentationTestMethod]
	public void ForegroundIsInheritedByCommandBarToggleButtonPathIconThroughThemeTemplate()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var icon = new PathIcon();
		var btn = new CommandBarToggleButton
		{
			Icon = icon
		};
		var commandBar = new CommandBar
		{
			Foreground = Brushes.Red
		};
		commandBar.PrimaryCommands.Add(btn);

		CornerstoneTest.Same(icon, ApplyCornerstoneThemeAndGetPresentedPathIcon(commandBar, btn));
		CornerstoneTest.Same(Brushes.Red, icon.Foreground);

		commandBar.Foreground = Brushes.Blue;

		CornerstoneTest.Same(Brushes.Blue, icon.Foreground);
	}

	[PresentationTestMethod]
	public void HasSecondaryCommandsDefaultIsFalse()
	{
		CornerstoneTest.IsFalse(new CommandBar().HasSecondaryCommands);
	}

	[PresentationTestMethod]
	public void IsDynamicOverflowEnabledDefaultIsFalse()
	{
		CornerstoneTest.IsFalse(new CommandBar().IsDynamicOverflowEnabled);
	}

	[PresentationTestMethod]
	public void IsOpenDefaultIsFalse()
	{
		CornerstoneTest.IsFalse(new CommandBar().IsOpen);
	}

	[PresentationTestMethod]
	public void IsOverflowButtonVisibleDefaultIsFalse()
	{
		CornerstoneTest.IsFalse(new CommandBar().IsOverflowButtonVisible);
	}

	[PresentationTestMethod]
	public void IsStickyDefaultIsFalse()
	{
		CornerstoneTest.IsFalse(new CommandBar().IsSticky);
	}

	[PresentationTestMethod]
	public void ItemWidthBottomDefaultIs70()
	{
		CornerstoneTest.AreEqual(70d, new CommandBar().ItemWidthBottom);
	}

	[PresentationTestMethod]
	public void ItemWidthCollapsedDefaultIs42()
	{
		CornerstoneTest.AreEqual(42d, new CommandBar().ItemWidthCollapsed);
	}

	[PresentationTestMethod]
	public void ItemWidthRightDefaultIs102()
	{
		CornerstoneTest.AreEqual(102d, new CommandBar().ItemWidthRight);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilityDefaultIsAuto()
	{
		CornerstoneTest.AreEqual(CommandBarOverflowButtonVisibility.Auto, new CommandBar().OverflowButtonVisibility);
	}

	[PresentationTestMethod]
	public void OverflowItemsNotNull()
	{
		CornerstoneTest.IsNotNull(new CommandBar().OverflowItems);
	}

	[PresentationTestMethod]
	public void OverflowItemsStartsEmpty()
	{
		CornerstoneTest.Empty(new CommandBar().OverflowItems);
	}

	[PresentationTestMethod]
	public void PrimaryCommandsNotNull()
	{
		CornerstoneTest.IsNotNull(new CommandBar().PrimaryCommands);
	}

	[PresentationTestMethod]
	public void PrimaryCommandsReturnsNewListWhenNull()
	{
		var cb = new CommandBar();
		cb.ClearValue(CommandBar.PrimaryCommandsProperty);
		CornerstoneTest.IsNotNull(cb.PrimaryCommands);
		CornerstoneTest.Empty(cb.PrimaryCommands);
	}

	[PresentationTestMethod]
	public void PrimaryCommandsStartsEmpty()
	{
		CornerstoneTest.Empty(new CommandBar().PrimaryCommands);
	}

	[PresentationTestMethod]
	public void SecondaryCommandsNotNull()
	{
		CornerstoneTest.IsNotNull(new CommandBar().SecondaryCommands);
	}

	[PresentationTestMethod]
	public void SecondaryCommandsReturnsNewListWhenNull()
	{
		var cb = new CommandBar();
		cb.ClearValue(CommandBar.SecondaryCommandsProperty);
		CornerstoneTest.IsNotNull(cb.SecondaryCommands);
		CornerstoneTest.Empty(cb.SecondaryCommands);
	}

	[PresentationTestMethod]
	public void SecondaryCommandsStartsEmpty()
	{
		CornerstoneTest.Empty(new CommandBar().SecondaryCommands);
	}

	[PresentationTestMethod]
	public void VisiblePrimaryCommandsNotNull()
	{
		CornerstoneTest.IsNotNull(new CommandBar().VisiblePrimaryCommands);
	}

	[PresentationTestMethod]
	public void VisiblePrimaryCommandsStartsEmpty()
	{
		CornerstoneTest.Empty(new CommandBar().VisiblePrimaryCommands);
	}

	private static PathIcon ApplyCornerstoneThemeAndGetPresentedPathIcon(CommandBar commandBar, TemplatedControl command)
	{
		var simpleTheme = new CornerstoneTheme();
		CornerstoneTest.IsTrue(simpleTheme.TryGetResource(typeof(CommandBar), ThemeVariant.Default, out var commandBarTheme));
		CornerstoneTest.IsTrue(simpleTheme.TryGetResource(command.GetType(), ThemeVariant.Default, out var commandTheme));
		commandBar.Theme = CornerstoneTest.IsType<ControlTheme>(commandBarTheme);
		command.Theme = CornerstoneTest.IsType<ControlTheme>(commandTheme);

		var root = new TestRoot
		{
			Width = 500,
			Height = 200,
			Child = commandBar,
			Styles =
			{
				simpleTheme
			}
		};

		root.ApplyStyling();
		commandBar.ApplyStyling();
		commandBar.ApplyTemplate();
		root.LayoutManager.ExecuteInitialLayoutPass();

		command.ApplyStyling();
		command.ApplyTemplate();

		var presenter = command.GetTemplateDescendants()
			.OfType<ContentPresenter>()
			.Single(x => x.Name == "PART_IconPresenter");

		presenter.ApplyStyling();
		presenter.UpdateChild();

		var pathIcon = CornerstoneTest.IsType<PathIcon>(presenter.Child);
		pathIcon.ApplyStyling();

		return pathIcon;
	}

	#endregion
}

[TestClass]
public class CommandBarPropertyRoundTripTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ContentRoundTrip()
	{
		var cb = new CommandBar();
		var content = new object();
		cb.Content = content;
		CornerstoneTest.Same(content, cb.Content);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionRoundTrip()
	{
		var cb = new CommandBar { DefaultLabelPosition = CommandBarDefaultLabelPosition.Right };
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Right, cb.DefaultLabelPosition);
	}

	[PresentationTestMethod]
	public void IsDynamicOverflowEnabledRoundTrip()
	{
		var cb = new CommandBar { IsDynamicOverflowEnabled = true };
		CornerstoneTest.IsTrue(cb.IsDynamicOverflowEnabled);
	}

	[PresentationTestMethod]
	public void IsOpenRoundTrip()
	{
		var cb = new CommandBar { IsOpen = true };
		CornerstoneTest.IsTrue(cb.IsOpen);
		cb.IsOpen = false;
		CornerstoneTest.IsFalse(cb.IsOpen);
	}

	[PresentationTestMethod]
	public void IsStickyRoundTrip()
	{
		var cb = new CommandBar { IsSticky = true };
		CornerstoneTest.IsTrue(cb.IsSticky);
	}

	[PresentationTestMethod]
	public void ItemWidthBottomRoundTrip()
	{
		var cb = new CommandBar { ItemWidthBottom = 80d };
		CornerstoneTest.AreEqual(80d, cb.ItemWidthBottom);
	}

	[PresentationTestMethod]
	public void ItemWidthCollapsedRoundTrip()
	{
		var cb = new CommandBar { ItemWidthCollapsed = 50d };
		CornerstoneTest.AreEqual(50d, cb.ItemWidthCollapsed);
	}

	[PresentationTestMethod]
	public void ItemWidthRightRoundTrip()
	{
		var cb = new CommandBar { ItemWidthRight = 120d };
		CornerstoneTest.AreEqual(120d, cb.ItemWidthRight);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilityRoundTrip()
	{
		var cb = new CommandBar { OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible };
		CornerstoneTest.AreEqual(CommandBarOverflowButtonVisibility.Visible, cb.OverflowButtonVisibility);
	}

	#endregion
}

[TestClass]
public class CommandBarIsOpenTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ClosedFiredWhenIsOpenBecomesFalse()
	{
		var cb = new CommandBar { IsOpen = true };
		var fired = false;
		cb.Closed += (_, _) => fired = true;
		cb.IsOpen = false;
		CornerstoneTest.IsTrue(fired);
	}

	[PresentationTestMethod]
	public void ClosingFiredWhenIsOpenBecomesFalse()
	{
		var cb = new CommandBar { IsOpen = true };
		var fired = false;
		cb.Closing += (_, _) => fired = true;
		cb.IsOpen = false;
		CornerstoneTest.IsTrue(fired);
	}

	[PresentationTestMethod]
	public void ClosingNotFiredWhenAlreadyClosed()
	{
		var cb = new CommandBar();
		var count = 0;
		cb.Closing += (_, _) => count++;
		cb.IsOpen = false;
		CornerstoneTest.AreEqual(0, count);
	}

	[PresentationTestMethod]
	public void EventsFiredInOrderOpenThenClose()
	{
		var cb = new CommandBar();
		var events = new List<string>();
		cb.Opening += (_, _) => events.Add("Opening");
		cb.Opened += (_, _) => events.Add("Opened");
		cb.Closing += (_, _) => events.Add("Closing");
		cb.Closed += (_, _) => events.Add("Closed");

		cb.IsOpen = true;
		cb.IsOpen = false;

		CornerstoneTest.AreEqual(new[] { "Opening", "Opened", "Closing", "Closed" }, events);
	}

	[PresentationTestMethod]
	public void OpenedFiredWhenIsOpenBecomesTrue()
	{
		var cb = new CommandBar();
		var fired = false;
		cb.Opened += (_, _) => fired = true;
		cb.IsOpen = true;
		CornerstoneTest.IsTrue(fired);
	}

	[PresentationTestMethod]
	public void OpeningFiredWhenIsOpenBecomesTrue()
	{
		var cb = new CommandBar();
		var fired = false;
		cb.Opening += (_, _) => fired = true;
		cb.IsOpen = true;
		CornerstoneTest.IsTrue(fired);
	}

	[PresentationTestMethod]
	public void OpeningNotFiredWhenAlreadyOpen()
	{
		var cb = new CommandBar();
		cb.IsOpen = true;
		var count = 0;
		cb.Opening += (_, _) => count++;
		cb.IsOpen = true;
		CornerstoneTest.AreEqual(0, count);
	}

	#endregion
}

[TestClass]
public class CommandBarCollectionTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CommandBarSeparatorCanBeAddedToPrimaryCommands()
	{
		var cb = new CommandBar();
		var sep = new CommandBarSeparator();
		cb.PrimaryCommands!.Add(sep);
		CornerstoneTest.Contains(cb.VisiblePrimaryCommands, sep);
	}

	[PresentationTestMethod]
	public void CommandBarToggleButtonCanBeAddedToPrimaryCommands()
	{
		var cb = new CommandBar();
		var toggle = new CommandBarToggleButton { Label = "Bold" };
		cb.PrimaryCommands!.Add(toggle);
		CornerstoneTest.Contains(cb.VisiblePrimaryCommands, toggle);
	}

	[PresentationTestMethod]
	public void HasSecondaryCommandsFalseAfterSecondaryCleared()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton { Label = "Options" };
		cb.SecondaryCommands!.Add(btn);
		cb.SecondaryCommands!.Remove(btn);
		CornerstoneTest.IsFalse(cb.HasSecondaryCommands);
	}

	[PresentationTestMethod]
	public void HasSecondaryCommandsTrueWhenSecondaryAdded()
	{
		var cb = new CommandBar();
		cb.SecondaryCommands!.Add(new CommandBarButton { Label = "Options" });
		CornerstoneTest.IsTrue(cb.HasSecondaryCommands);
	}

	[PresentationTestMethod]
	public void MultiplePrimaryCommandsAllVisibleInOrder()
	{
		var cb = new CommandBar();
		var btn1 = new CommandBarButton { Label = "A" };
		var btn2 = new CommandBarButton { Label = "B" };
		var btn3 = new CommandBarButton { Label = "C" };
		cb.PrimaryCommands!.Add(btn1);
		cb.PrimaryCommands!.Add(btn2);
		cb.PrimaryCommands!.Add(btn3);
		CornerstoneTest.AreEqual(new ICommandBarElement[] { btn1, btn2, btn3 }, cb.VisiblePrimaryCommands);
	}

	[PresentationTestMethod]
	public void OverflowItemsCountMatchesSecondaryCommandCount()
	{
		var cb = new CommandBar();
		cb.SecondaryCommands!.Add(new CommandBarButton());
		cb.SecondaryCommands!.Add(new CommandBarButton());
		CornerstoneTest.AreEqual(2, cb.OverflowItems.Count);
	}

	[PresentationTestMethod]
	public void PrimaryCommandsAddedAppearInVisiblePrimaryWhenDynamicOverflowDisabled()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton { Label = "Save" };
		cb.PrimaryCommands!.Add(btn);
		CornerstoneTest.Contains(cb.VisiblePrimaryCommands, btn);
	}

	[PresentationTestMethod]
	public void PrimaryCommandsDefaultCollectionDoesNotDuplicateVisiblePrimaryNotifications()
	{
		var cb = new CommandBar();
		var notifications = 0;

		((INotifyCollectionChanged) cb.VisiblePrimaryCommands).CollectionChanged += (_, _) => notifications++;

		cb.PrimaryCommands!.Add(new CommandBarButton { Label = "Save" });

		CornerstoneTest.AreEqual(2, notifications);
	}

	[PresentationTestMethod]
	public void PrimaryCommandsRemovedDisappearsFromVisiblePrimary()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton { Label = "Save" };
		cb.PrimaryCommands!.Add(btn);
		cb.PrimaryCommands!.Remove(btn);
		CornerstoneTest.DoesNotContain(cb.VisiblePrimaryCommands, btn);
	}

	[PresentationTestMethod]
	public void SecondaryCommandsAddedAppearInOverflowItems()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton { Label = "Settings" };
		cb.SecondaryCommands!.Add(btn);
		CornerstoneTest.Contains(cb.OverflowItems, btn);
	}

	[PresentationTestMethod]
	public void SecondaryCommandsDefaultCollectionDoesNotDuplicateOverflowNotifications()
	{
		var cb = new CommandBar();
		var notifications = 0;

		((INotifyCollectionChanged) cb.OverflowItems).CollectionChanged += (_, _) => notifications++;

		cb.SecondaryCommands!.Add(new CommandBarButton { Label = "Settings" });

		CornerstoneTest.AreEqual(2, notifications);
	}

	[PresentationTestMethod]
	public void SecondaryCommandsRemovedDisappearsFromOverflowItems()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton { Label = "Settings" };
		cb.SecondaryCommands!.Add(btn);
		cb.SecondaryCommands!.Remove(btn);
		CornerstoneTest.DoesNotContain(cb.OverflowItems, btn);
	}

	[PresentationTestMethod]
	public void VisiblePrimaryCommandsCountMatchesPrimaryWhenDynamicOverflowDisabled()
	{
		var cb = new CommandBar();
		cb.PrimaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands!.Add(new CommandBarButton());
		CornerstoneTest.AreEqual(2, cb.VisiblePrimaryCommands.Count);
	}

	#endregion
}

[TestClass]
public class CommandBarLabelPositionTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void DefaultLabelPositionBottomClearsIsCompactOnPrimaryButton()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton();
		cb.PrimaryCommands!.Add(btn);
		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Bottom;

		CornerstoneTest.IsFalse(btn.IsCompact);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionCollapsedDoesNotCompactOverflowedPrimaryCommand()
	{
		var cb = new CommandBar
		{
			DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed,
			IsDynamicOverflowEnabled = true
		};
		cb.Measure(new Size(81, double.PositiveInfinity));

		var visible = new CommandBarButton();
		var overflowed = new CommandBarButton();
		cb.PrimaryCommands!.Add(visible);
		cb.PrimaryCommands!.Add(overflowed);

		CornerstoneTest.Contains(cb.VisiblePrimaryCommands, visible);
		CornerstoneTest.IsTrue(visible.IsCompact);
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Collapsed, visible.LabelPosition);
		CornerstoneTest.Contains(cb.OverflowItems, overflowed);
		CornerstoneTest.IsTrue(overflowed.IsInOverflow);
		CornerstoneTest.IsFalse(overflowed.IsCompact);
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Right, overflowed.LabelPosition);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionCollapsedDoesNotCompactSecondaryCommands()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton();
		cb.SecondaryCommands!.Add(btn);

		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

		CornerstoneTest.IsTrue(btn.IsInOverflow);
		CornerstoneTest.IsFalse(btn.IsCompact);
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Right, btn.LabelPosition);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionCollapsedDoesNotCompactSecondaryToggleButton()
	{
		var cb = new CommandBar();
		var toggle = new CommandBarToggleButton();
		cb.SecondaryCommands!.Add(toggle);

		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

		CornerstoneTest.IsTrue(toggle.IsInOverflow);
		CornerstoneTest.IsFalse(toggle.IsCompact);
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Right, toggle.LabelPosition);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionCollapsedPropagatesIsCompactToToggleButton()
	{
		var cb = new CommandBar();
		var toggle = new CommandBarToggleButton();
		cb.PrimaryCommands!.Add(toggle);

		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

		CornerstoneTest.IsTrue(toggle.IsCompact);
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Collapsed, toggle.LabelPosition);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionCollapsedSetsIsCompactOnExistingPrimaryButton()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton();
		cb.PrimaryCommands!.Add(btn);

		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

		CornerstoneTest.IsTrue(btn.IsCompact);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionCollapsedSetsIsCompactOnSeparator()
	{
		var cb = new CommandBar();
		var sep = new CommandBarSeparator();
		cb.PrimaryCommands!.Add(sep);

		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

		CornerstoneTest.IsTrue(sep.IsCompact);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionCollapsedSetsLabelPositionOnPrimaryButton()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton();
		cb.PrimaryCommands!.Add(btn);

		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Collapsed, btn.LabelPosition);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionDoesNotClearLabelText()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton { Label = "Save" };
		cb.PrimaryCommands!.Add(btn);

		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;

		CornerstoneTest.AreEqual("Save", btn.Label);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionRightPropagatesLabelPositionToToggleButton()
	{
		var cb = new CommandBar();
		var toggle = new CommandBarToggleButton();
		cb.PrimaryCommands!.Add(toggle);

		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;

		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Right, toggle.LabelPosition);
	}

	[PresentationTestMethod]
	public void DefaultLabelPositionRightSetsLabelPositionOnPrimaryButton()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton();
		cb.PrimaryCommands!.Add(btn);

		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;

		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Right, btn.LabelPosition);
	}

	[PresentationTestMethod]
	public void NewPrimaryCommandGetsCurrentLabelPositionWhenAlreadyCollapsed()
	{
		var cb = new CommandBar { DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed };

		var btn = new CommandBarButton();
		cb.PrimaryCommands!.Add(btn);

		CornerstoneTest.IsTrue(btn.IsCompact);
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Collapsed, btn.LabelPosition);
	}

	[PresentationTestMethod]
	public void NewPrimaryCommandGetsCurrentLabelPositionWhenRight()
	{
		var cb = new CommandBar { DefaultLabelPosition = CommandBarDefaultLabelPosition.Right };

		var btn = new CommandBarButton();
		cb.PrimaryCommands!.Add(btn);

		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Right, btn.LabelPosition);
	}

	[PresentationTestMethod]
	public void NewSecondaryCommandGetsOverflowLabelPositionWhenAlreadyCollapsed()
	{
		var cb = new CommandBar { DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed };

		var btn = new CommandBarButton();
		cb.SecondaryCommands!.Add(btn);

		CornerstoneTest.IsTrue(btn.IsInOverflow);
		CornerstoneTest.IsFalse(btn.IsCompact);
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Right, btn.LabelPosition);
	}

	[PresentationTestMethod]
	public void OverflowedPrimaryCommandReappliesDefaultLabelPositionWhenRestored()
	{
		var cb = new CommandBar
		{
			DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed,
			IsDynamicOverflowEnabled = true
		};
		cb.Measure(new Size(81, double.PositiveInfinity));

		var visible = new CommandBarButton();
		var overflowed = new CommandBarButton();
		cb.PrimaryCommands!.Add(visible);
		cb.PrimaryCommands!.Add(overflowed);
		CornerstoneTest.Contains(cb.OverflowItems, overflowed);

		cb.Measure(new Size(400, double.PositiveInfinity));

		CornerstoneTest.Contains(cb.VisiblePrimaryCommands, overflowed);
		CornerstoneTest.IsFalse(overflowed.IsInOverflow);
		CornerstoneTest.IsTrue(overflowed.IsCompact);
		CornerstoneTest.AreEqual(CommandBarDefaultLabelPosition.Collapsed, overflowed.LabelPosition);
	}

	#endregion
}

[TestClass]
public class CommandBarOverflowButtonTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void OverflowButtonVisibilityAutoFalseAfterSecondaryRemoved()
	{
		var cb = new CommandBar();
		var btn = new CommandBarButton();
		cb.SecondaryCommands!.Add(btn);
		CornerstoneTest.IsTrue(cb.IsOverflowButtonVisible);

		cb.SecondaryCommands!.Remove(btn);
		CornerstoneTest.IsFalse(cb.IsOverflowButtonVisible);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilityAutoFalseWhenNoSecondaryCommands()
	{
		var cb = new CommandBar();
		CornerstoneTest.IsFalse(cb.IsOverflowButtonVisible);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilityAutoTrueWhenHasSecondaryCommands()
	{
		var cb = new CommandBar();
		cb.SecondaryCommands!.Add(new CommandBarButton());
		CornerstoneTest.IsTrue(cb.IsOverflowButtonVisible);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilityCollapsedRemainsFalseEvenWithSecondary()
	{
		var cb = new CommandBar { OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed };
		cb.SecondaryCommands!.Add(new CommandBarButton());
		CornerstoneTest.IsFalse(cb.IsOverflowButtonVisible);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilityCollapsedSetsIsOverflowButtonVisibleFalse()
	{
		var cb = new CommandBar { OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed };
		CornerstoneTest.IsFalse(cb.IsOverflowButtonVisible);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilitySwitchFromAutoToVisibleShowsButtonImmediately()
	{
		var cb = new CommandBar();
		CornerstoneTest.IsFalse(cb.IsOverflowButtonVisible);

		cb.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible;
		CornerstoneTest.IsTrue(cb.IsOverflowButtonVisible);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilityVisibleRemainsTrueWithoutSecondary()
	{
		var cb = new CommandBar { OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible };
		CornerstoneTest.IsTrue(cb.IsOverflowButtonVisible);
	}

	[PresentationTestMethod]
	public void OverflowButtonVisibilityVisibleSetsIsOverflowButtonVisibleTrue()
	{
		var cb = new CommandBar { OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible };
		CornerstoneTest.IsTrue(cb.IsOverflowButtonVisible);
	}

	#endregion
}

[TestClass]
public class CommandBarItemWidthTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ItemWidthBottomControlsHowManyButtonsFit()
	{
		var cb = CreateWithWidth(300);
		var secondary = new CommandBarButton();
		cb.SecondaryCommands!.Add(secondary); // forces overflow button
		for (var i = 0; i < 4; i++)
		{
			cb.PrimaryCommands!.Add(new CommandBarButton());
		}
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(3, cb.VisiblePrimaryCommands.Count);
		CornerstoneTest.AreEqual(3, cb.OverflowItems.Count);
		CornerstoneTest.IsType<CommandBarButton>(cb.OverflowItems[0]);
		CornerstoneTest.IsType<CommandBarSeparator>(cb.OverflowItems[1]);
		CornerstoneTest.Same(secondary, cb.OverflowItems[2]);
	}

	[PresentationTestMethod]
	public void ItemWidthBottomLargeReducesToMinimumOneVisible()
	{
		var cb = CreateWithWidth(300);
		cb.ItemWidthBottom = 260;
		cb.SecondaryCommands!.Add(new CommandBarButton());
		for (var i = 0; i < 3; i++)
		{
			cb.PrimaryCommands!.Add(new CommandBarButton());
		}
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(1, cb.VisiblePrimaryCommands.Count);
	}

	[PresentationTestMethod]
	public void ItemWidthBottomReducedAllowsMoreItemsToFit()
	{
		var cb = CreateWithWidth(300);
		cb.ItemWidthBottom = 35;
		cb.SecondaryCommands!.Add(new CommandBarButton());
		for (var i = 0; i < 4; i++)
		{
			cb.PrimaryCommands!.Add(new CommandBarButton());
		}
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(4, cb.VisiblePrimaryCommands.Count);
	}

	[PresentationTestMethod]
	public void ItemWidthCollapsedUsedWhenLabelPositionIsCollapsed()
	{
		var cb = CreateWithWidth(300);
		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;
		cb.SecondaryCommands!.Add(new CommandBarButton());
		for (var i = 0; i < 4; i++)
		{
			cb.PrimaryCommands!.Add(new CommandBarButton());
		}
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(4, cb.VisiblePrimaryCommands.Count);
	}

	[PresentationTestMethod]
	public void ItemWidthRightIncreasedFewerItemsFit()
	{
		var cb = CreateWithWidth(300);
		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
		cb.ItemWidthRight = 252; // exactly 1 fits: 252/252=1
		cb.SecondaryCommands!.Add(new CommandBarButton());
		for (var i = 0; i < 3; i++)
		{
			cb.PrimaryCommands!.Add(new CommandBarButton());
		}
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(1, cb.VisiblePrimaryCommands.Count);
	}

	[PresentationTestMethod]
	public void ItemWidthRightUsedWhenLabelPositionIsRight()
	{
		var cb = CreateWithWidth(300);
		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
		cb.SecondaryCommands!.Add(new CommandBarButton());
		for (var i = 0; i < 4; i++)
		{
			cb.PrimaryCommands!.Add(new CommandBarButton());
		}
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(2, cb.VisiblePrimaryCommands.Count);
	}

	[PresentationTestMethod]
	public void ItemWidthsAreIndependentPerLabelPosition()
	{
		var cb = CreateWithWidth(300);
		cb.ItemWidthBottom = 70;
		cb.ItemWidthRight = 102;
		cb.ItemWidthCollapsed = 42;
		cb.SecondaryCommands!.Add(new CommandBarButton());
		for (var i = 0; i < 4; i++)
		{
			cb.PrimaryCommands!.Add(new CommandBarButton());
		}

		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Bottom;
		cb.IsDynamicOverflowEnabled = true;
		var visibleBottom = cb.VisiblePrimaryCommands.Count; // 252/70 = 3

		cb.IsDynamicOverflowEnabled = false;
		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Right;
		cb.IsDynamicOverflowEnabled = true;
		var visibleRight = cb.VisiblePrimaryCommands.Count; // 252/102 = 2

		cb.IsDynamicOverflowEnabled = false;
		cb.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;
		cb.IsDynamicOverflowEnabled = true;
		var visibleCollapsed = cb.VisiblePrimaryCommands.Count; // 252/42 = 6 → capped at 4

		CornerstoneTest.AreEqual(3, visibleBottom);
		CornerstoneTest.AreEqual(2, visibleRight);
		CornerstoneTest.AreEqual(4, visibleCollapsed);
	}

	private static CommandBar CreateWithWidth(double width)
	{
		var cb = new CommandBar();
		cb.Measure(new Size(width, double.PositiveInfinity));
		return cb;
	}

	#endregion
}

[TestClass]
public class CommandBarOverflowKeyboardTests : ScopedTestBase
{
	#region Fields

	private readonly IDisposable _app;

	#endregion

	#region Constructors

	public CommandBarOverflowKeyboardTests()
	{
		_app = UnitTestApplication.Start(TestServices.FocusableWindow);
	}

	#endregion

	#region Methods

	public override void Dispose()
	{
		_app.Dispose();
		base.Dispose();
	}

	[PresentationTestMethod]
	public void EscapeWhenOverflowClosedDoesNothing()
	{
		var cb = new CommandBar();
		cb.SecondaryCommands.Add(new CommandBarButton { Label = "Action" });
		var root = new TestRoot(true, cb);
		root.LayoutManager.ExecuteInitialLayoutPass();

		RaiseKeyOnOverflowPresenter(cb, Key.Escape);

		CornerstoneTest.IsFalse(cb.IsOpen);
	}

	[PresentationTestMethod]
	public void EscapeWhenOverflowOpenClosesOverflow()
	{
		var cb = new CommandBar();
		cb.SecondaryCommands.Add(new CommandBarButton { Label = "Action" });
		var root = new TestRoot(true, cb);
		root.LayoutManager.ExecuteInitialLayoutPass();
		cb.IsOpen = true;

		RaiseKeyOnOverflowPresenter(cb, Key.Escape);

		CornerstoneTest.IsFalse(cb.IsOpen);
	}

	[PresentationTestMethod]
	public void KeyboardOpenAfterPointerFocusMakesFirstOverflowItemFocusVisible()
	{
		var first = new CommandBarButton { Label = "A" };
		var cb = new CommandBar();
		cb.SecondaryCommands.Add(first);
		var window = CreateWindow(cb);

		var overflowButton = GetOverflowButton(cb);
		CornerstoneTest.IsTrue(overflowButton.Focus(NavigationMethod.Pointer));
		overflowButton.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Space
		});

		cb.IsOpen = true;
		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

		CornerstoneTest.IsTrue(first.IsFocused);
		CornerstoneTest.IsTrue(first.Classes.Contains(":focus-visible"));
	}

	[PresentationTestMethod]
	public void KeyboardOpenFocusesFirstOverflowItemAsFocusVisible()
	{
		var first = new CommandBarButton { Label = "A" };
		var cb = new CommandBar();
		cb.SecondaryCommands.Add(first);
		var window = CreateWindow(cb);

		CornerstoneTest.IsTrue(GetOverflowButton(cb).Focus(NavigationMethod.Tab));

		cb.IsOpen = true;
		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

		CornerstoneTest.IsTrue(first.IsFocused);
		CornerstoneTest.IsTrue(first.Classes.Contains(":focus-visible"));
	}

	[PresentationTestMethod]
	public void NavigationKeysAreHandledWhenAllItemsAreDisabled()
	{
		var cb = new CommandBar();
		cb.SecondaryCommands.Add(new CommandBarButton { Label = "A", IsEnabled = false });
		var root = new TestRoot(true, cb);
		root.LayoutManager.ExecuteInitialLayoutPass();
		cb.IsOpen = true;

		var e = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down };
		GetOverflowPresenter(cb).RaiseEvent(e);

		CornerstoneTest.IsTrue(e.Handled);
	}

	[PresentationTestMethod]
	public void NavigationKeysAreHandledWhenAllItemsAreNonFocusable()
	{
		var cb = new CommandBar();
		cb.SecondaryCommands.Add(new CommandBarButton { Label = "A", Focusable = false });
		var root = new TestRoot(true, cb);
		root.LayoutManager.ExecuteInitialLayoutPass();
		cb.IsOpen = true;

		var e = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down };
		GetOverflowPresenter(cb).RaiseEvent(e);

		CornerstoneTest.IsTrue(e.Handled);
	}

	[PresentationTestMethod]
	public void NavigationKeysAreHandledWhenAllItemsAreSeparators()
	{
		var cb = new CommandBar();
		cb.SecondaryCommands.Add(new CommandBarSeparator());
		var root = new TestRoot(true, cb);
		root.LayoutManager.ExecuteInitialLayoutPass();
		cb.IsOpen = true;

		var e = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down };
		GetOverflowPresenter(cb).RaiseEvent(e);

		CornerstoneTest.IsTrue(e.Handled);
	}

	[PresentationTestMethod]
	[DataRow(Key.Down)]
	[DataRow(Key.Up)]
	[DataRow(Key.Home)]
	[DataRow(Key.End)]
	public void NavigationKeysAreHandledWhenOverflowHasItems(Key key)
	{
		var cb = new CommandBar();
		cb.SecondaryCommands.Add(new CommandBarButton { Label = "A" });
		cb.SecondaryCommands.Add(new CommandBarButton { Label = "B" });
		var root = new TestRoot(true, cb);
		root.LayoutManager.ExecuteInitialLayoutPass();
		cb.IsOpen = true;

		var e = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key };
		GetOverflowPresenter(cb).RaiseEvent(e);

		CornerstoneTest.IsTrue(e.Handled);
	}

	[PresentationTestMethod]
	public void OpenDoesNotThrowWhenSecondaryCommandVisibilityChangesDuringOverflowRealization()
	{
		var secondary = new VisibilityChangingCommandBarButton { Label = "Bound" };
		var cb = new CommandBar();
		cb.SecondaryCommands.Add(secondary);
		var window = CreateWindow(cb);

		cb.IsOpen = true;

		CornerstoneTest.IsTrue(cb.IsOpen);
		CornerstoneTest.IsFalse(secondary.IsVisible);
	}

	[PresentationTestMethod]
	public void PointerOpenAfterKeyboardFocusDoesNotMakeFirstOverflowItemFocusVisible()
	{
		var first = new CommandBarButton { Label = "A" };
		var cb = new CommandBar();
		cb.SecondaryCommands.Add(first);
		var window = CreateWindow(cb);

		var overflowButton = GetOverflowButton(cb);
		CornerstoneTest.IsTrue(overflowButton.Focus(NavigationMethod.Tab));
		RaisePointerPressed(overflowButton);

		cb.IsOpen = true;
		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

		CornerstoneTest.IsTrue(first.IsFocused);
		CornerstoneTest.IsFalse(first.Classes.Contains(":focus-visible"));
	}

	private static Window CreateWindow(Control content)
	{
		var window = new Window { Content = content };
		window.Show();
		window.ApplyStyling();
		window.ApplyTemplate();
		return window;
	}

	private static Button GetOverflowButton(CommandBar cb)
	{
		return cb.GetVisualDescendants()
			.OfType<Button>()
			.First(x => x.Name == "PART_OverflowButton");
	}

	private static ItemsControl GetOverflowPresenter(CommandBar cb)
	{
		var popup = cb.GetVisualDescendants()
			.OfType<Popup>()
			.First(p => p.Name == "PART_OverflowPopup");

		return popup.GetLogicalDescendants()
			.OfType<ItemsControl>()
			.First(x => x.Name == "PART_OverflowPresenter");
	}

	private static void RaiseKeyOnOverflowPresenter(CommandBar cb, Key key)
	{
		GetOverflowPresenter(cb).RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = key
		});
	}

	private static void RaisePointerPressed(Button target)
	{
		var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
		target.RaiseEvent(new PointerPressedEventArgs(
			target,
			pointer,
			target,
			default,
			1,
			new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
			KeyModifiers.None));
	}

	#endregion

	#region Classes

	private class VisibilityChangingCommandBarButton : CommandBarButton
	{
		#region Fields

		private bool _hasUpdatedVisibility;

		#endregion

		#region Methods

		protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
		{
			base.OnPropertyChanged(change);

			if (!_hasUpdatedVisibility &&
				(change.Property == ParentProperty) &&
				change.NewValue is not null)
			{
				_hasUpdatedVisibility = true;
				SetCurrentValue(IsVisibleProperty, false);
			}
		}

		#endregion
	}

	#endregion
}

[TestClass]
public class CommandBarSeparatorOverflowTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AllButtonsOverflowSeparatorsAlsoOverflow()
	{
		// [Sep, Btn, Btn] with room for 0: everything overflows.
		var cb = CreateWithWidth(50);
		cb.SecondaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands!.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.Empty(cb.VisiblePrimaryCommands);
	}

	[PresentationTestMethod]
	public void ConsecutiveSeparatorsCollapsedToOne()
	{
		// [Btn, Sep, Sep, Btn] all fit: only one separator should remain.
		var cb = CreateWithWidth(300);
		cb.PrimaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.IsDynamicOverflowEnabled = true;

		var sepCount = CountSeparators(cb.VisiblePrimaryCommands);
		CornerstoneTest.AreEqual(1, sepCount);
	}

	[PresentationTestMethod]
	public void HiddenSecondaryCommandsDoNotGetSyntheticOverflowSeparator()
	{
		var cb = CreateWithWidth(300);
		cb.ItemWidthBottom = 260;

		var visiblePrimary = new CommandBarButton();
		var overflowedPrimary = new CommandBarButton();
		var hiddenSecondary = new CommandBarButton { IsVisible = false };

		cb.PrimaryCommands!.Add(visiblePrimary);
		cb.PrimaryCommands.Add(overflowedPrimary);
		cb.SecondaryCommands!.Add(hiddenSecondary);
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(2, cb.OverflowItems.Count);
		CornerstoneTest.Same(overflowedPrimary, cb.OverflowItems[0]);
		CornerstoneTest.Same(hiddenSecondary, cb.OverflowItems[1]);
		CornerstoneTest.DoesNotContain(cb.OverflowItems, x => x is CommandBarSeparator);
	}

	[PresentationTestMethod]
	public void LeadingSeparatorIsStrippedFromVisible()
	{
		// [Sep, Btn, Btn, Btn] with room for 2: leading Sep should be stripped.
		var cb = CreateWithWidth(300);
		cb.SecondaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands!.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.IsNotType<CommandBarSeparator>(cb.VisiblePrimaryCommands[0]);
	}

	[PresentationTestMethod]
	public void MidSeparatorStaysVisibleWhenButtonsOnBothSides()
	{
		// [Btn, Sep, Btn] with room for all: separator stays.
		var cb = CreateWithWidth(300);
		cb.PrimaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(3, cb.VisiblePrimaryCommands.Count);
		CornerstoneTest.IsType<CommandBarSeparator>(cb.VisiblePrimaryCommands[1]);
	}

	[PresentationTestMethod]
	public void MultipleSeparatorGroupsOnlyValidOnesRemain()
	{
		// [Btn, Sep, Btn, Sep, Btn, Sep, Btn] with room for 3:
		// last Btn overflows, last Sep becomes trailing, the rest stay.
		var cb = CreateWithWidth(300);
		cb.SecondaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.IsNotType<CommandBarSeparator>(cb.VisiblePrimaryCommands[^1]);
		CornerstoneTest.IsNotType<CommandBarSeparator>(cb.VisiblePrimaryCommands[0]);
	}

	[PresentationTestMethod]
	public void MultipleSeparatorsAllTrailingOnesStripped()
	{
		// [Btn, Sep, Sep, Btn] with room for 1: both trailing separators should be stripped.
		var cb = CreateWithWidth(300);
		cb.ItemWidthBottom = 260;
		cb.SecondaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(1, cb.VisiblePrimaryCommands.Count);
		CornerstoneTest.IsType<CommandBarButton>(cb.VisiblePrimaryCommands[0]);
	}

	[PresentationTestMethod]
	public void OnlySeparatorsAllOverflow()
	{
		// [Sep, Sep, Sep] with no buttons: all should overflow.
		var cb = CreateWithWidth(300);
		cb.SecondaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands!.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.Empty(cb.VisiblePrimaryCommands);
	}

	[PresentationTestMethod]
	public void OrphanedMidSeparatorRemovedWhenNeighborOverflows()
	{
		// [Btn1, Sep, Btn2, Sep, Btn3] with room for 2: Btn3 overflows,
		// second Sep becomes trailing and is removed. First Sep stays.
		var cb = CreateWithWidth(300);
		cb.ItemWidthBottom = 100;
		cb.SecondaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.IsNotType<CommandBarSeparator>(cb.VisiblePrimaryCommands[^1]);
		CornerstoneTest.AreEqual(1, CountSeparators(cb.VisiblePrimaryCommands));
	}

	[PresentationTestMethod]
	public void OverflowedPrimaryCommandsPrecedeSecondaryCommandsWithSyntheticSeparator()
	{
		var cb = CreateWithWidth(300);
		cb.ItemWidthBottom = 260;

		var visiblePrimary = new CommandBarButton();
		var originalPrimarySeparator = new CommandBarSeparator();
		var overflowedPrimaryOne = new CommandBarButton();
		var overflowedPrimaryTwo = new CommandBarButton();
		var secondary = new CommandBarButton();

		cb.PrimaryCommands!.Add(visiblePrimary);
		cb.PrimaryCommands.Add(originalPrimarySeparator);
		cb.PrimaryCommands.Add(overflowedPrimaryOne);
		cb.PrimaryCommands.Add(overflowedPrimaryTwo);
		cb.SecondaryCommands!.Add(secondary);
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(4, cb.OverflowItems.Count);
		CornerstoneTest.Same(overflowedPrimaryOne, cb.OverflowItems[0]);
		CornerstoneTest.Same(overflowedPrimaryTwo, cb.OverflowItems[1]);
		CornerstoneTest.IsType<CommandBarSeparator>(cb.OverflowItems[2]);
		CornerstoneTest.NotSame(originalPrimarySeparator, cb.OverflowItems[2]);
		CornerstoneTest.Same(secondary, cb.OverflowItems[3]);
		CornerstoneTest.DoesNotContain(cb.OverflowItems, originalPrimarySeparator);
	}

	[PresentationTestMethod]
	public void PrimarySeparatorIsRemovedInsteadOfBecomingFirstOverflowItem()
	{
		var cb = CreateWithWidth(300);
		cb.ItemWidthBottom = 260;

		var leadingSeparator = new CommandBarSeparator();
		var firstButton = new CommandBarButton();
		var overflowedButton = new CommandBarButton();

		cb.PrimaryCommands!.Add(leadingSeparator);
		cb.PrimaryCommands.Add(firstButton);
		cb.PrimaryCommands.Add(overflowedButton);
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.Single(cb.OverflowItems);
		CornerstoneTest.Same(overflowedButton, cb.OverflowItems[0]);
		CornerstoneTest.DoesNotContain(cb.OverflowItems, leadingSeparator);
	}

	[PresentationTestMethod]
	public void SeparatorBetweenOverflowedButtonsIsRemoved()
	{
		// [Btn1, Btn2, Sep, Btn3, Btn4] with room for 2: Btn3 and Btn4 overflow,
		// Sep has no non-separator after it in visible set, so it is removed.
		var cb = CreateWithWidth(300);
		cb.ItemWidthBottom = 100;
		cb.SecondaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(0, CountSeparators(cb.VisiblePrimaryCommands));
	}

	[PresentationTestMethod]
	public void TogglingSecondaryVisibilityRebuildsSyntheticOverflowSeparator()
	{
		var cb = CreateWithWidth(300);
		cb.ItemWidthBottom = 260;

		var visiblePrimary = new CommandBarButton();
		var overflowedPrimary = new CommandBarButton();
		var secondary = new CommandBarButton();

		cb.PrimaryCommands!.Add(visiblePrimary);
		cb.PrimaryCommands.Add(overflowedPrimary);
		cb.SecondaryCommands!.Add(secondary);
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(3, cb.OverflowItems.Count);
		CornerstoneTest.Same(overflowedPrimary, cb.OverflowItems[0]);
		CornerstoneTest.IsType<CommandBarSeparator>(cb.OverflowItems[1]);
		CornerstoneTest.Same(secondary, cb.OverflowItems[2]);

		secondary.IsVisible = false;

		CornerstoneTest.AreEqual(2, cb.OverflowItems.Count);
		CornerstoneTest.Same(overflowedPrimary, cb.OverflowItems[0]);
		CornerstoneTest.Same(secondary, cb.OverflowItems[1]);
		CornerstoneTest.DoesNotContain(cb.OverflowItems, x => x is CommandBarSeparator);

		secondary.IsVisible = true;

		CornerstoneTest.AreEqual(3, cb.OverflowItems.Count);
		CornerstoneTest.Same(overflowedPrimary, cb.OverflowItems[0]);
		CornerstoneTest.IsType<CommandBarSeparator>(cb.OverflowItems[1]);
		CornerstoneTest.Same(secondary, cb.OverflowItems[2]);
	}

	[PresentationTestMethod]
	public void TrailingSeparatorIsNotLastVisibleItem()
	{
		// [Btn, Btn, Sep, Btn] with room for 2 buttons: Sep should NOT trail.
		var cb = CreateWithWidth(300);
		cb.SecondaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.IsNotType<CommandBarSeparator>(cb.VisiblePrimaryCommands[^1]);
	}

	[PresentationTestMethod]
	public void TrailingSeparatorMovedToOverflow()
	{
		// [Btn, Sep, Btn, Btn] with room for 1 button: Sep after the single visible button should overflow.
		var cb = CreateWithWidth(300);
		cb.ItemWidthBottom = 260;
		cb.SecondaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands!.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarSeparator());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.PrimaryCommands.Add(new CommandBarButton());
		cb.IsDynamicOverflowEnabled = true;

		CornerstoneTest.AreEqual(1, cb.VisiblePrimaryCommands.Count);
		CornerstoneTest.IsType<CommandBarButton>(cb.VisiblePrimaryCommands[0]);
	}

	private static int CountSeparators(IReadOnlyList<ICommandBarElement> items)
	{
		var count = 0;
		for (var i = 0; i < items.Count; i++)
		{
			if (items[i] is CommandBarSeparator)
			{
				count++;
			}
		}
		return count;
	}

	private static CommandBar CreateWithWidth(double width)
	{
		var cb = new CommandBar();
		cb.Measure(new Size(width, double.PositiveInfinity));
		return cb;
	}

	#endregion
}

file sealed class DelegateCommand : ICommand
{
	#region Fields

	private readonly Func<object, bool> _canExecute;
	private readonly Action<object> _execute;

	#endregion

	#region Constructors

	public DelegateCommand(Action<object> execute, Func<object, bool> canExecute = null)
	{
		_execute = execute;
		_canExecute = canExecute ?? (_ => true);
	}

	#endregion

	#region Methods

	public bool CanExecute(object parameter)
	{
		return _canExecute(parameter);
	}

	public void Execute(object parameter)
	{
		_execute(parameter);
	}

	#endregion

	#region Events

	public event EventHandler CanExecuteChanged
	{
		add { }
		remove { }
	}

	#endregion
}