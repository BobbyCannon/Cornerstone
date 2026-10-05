#region References

using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsNthLastChild
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(2, 0, ":nth-last-child(2n)")]
	[DataRow(2, 1, ":nth-last-child(2n+1)")]
	[DataRow(1, 0, ":nth-last-child(1n)")]
	[DataRow(4, -1, ":nth-last-child(4n-1)")]
	[DataRow(0, 1, ":nth-last-child(1)")]
	[DataRow(0, -1, ":nth-last-child(-1)")]
	[DataRow(int.MaxValue, int.MinValue + 1, ":nth-last-child(2147483647n-2147483647)")]
	public void NotSelectorShouldHaveCorrectStringRepresentation(int step, int offset, string expected)
	{
		var target = default(Selector).NthLastChild(step, offset);

		CornerstoneTest.AreEqual(expected, target.ToString());
	}

	[PresentationTestMethod]
	public async Task NthChildDoesntMatchControlInPanelWithZeroStepWithNegativeOffset()
	{
		Border b1, b2, b3, b4;
		var panel = new StackPanel();
		panel.Children.AddRange(new[]
		{
			b1 = new Border(),
			b2 = new Border(),
			b3 = new Border(),
			b4 = new Border()
		});

		var target = default(Selector).NthLastChild(0, -2);

		CornerstoneTest.IsFalse(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b3).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b4).Activator!.Take(1));
	}

	[PresentationTestMethod]
	public void NthChildDoesntMatchControlOutOfPanelParent()
	{
		Border b1;
		var contentControl = new ContentControl();
		contentControl.Content = b1 = new Border();

		var target = default(Selector).NthLastChild(1, 0);

		CornerstoneTest.AreEqual(SelectorMatch.NeverThisInstance, target.Match(b1));
	}

	[PresentationTestMethod]
	public async Task NthChildMatchControlInPanel()
	{
		Border b1, b2, b3, b4;
		var panel = new StackPanel();
		panel.Children.AddRange(new[]
		{
			b1 = new Border(),
			b2 = new Border(),
			b3 = new Border(),
			b4 = new Border()
		});

		var target = default(Selector).NthLastChild(2, 0);

		CornerstoneTest.IsTrue(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b3).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b4).Activator!.Take(1));
	}

	[PresentationTestMethod]
	public async Task NthChildMatchControlInPanelWithNegativeOffset()
	{
		Border b1, b2, b3, b4;
		var panel = new StackPanel();
		panel.Children.AddRange(new[]
		{
			b1 = new Border(),
			b2 = new Border(),
			b3 = new Border(),
			b4 = new Border()
		});

		var target = default(Selector).NthLastChild(4, -1);

		CornerstoneTest.IsFalse(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b3).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b4).Activator!.Take(1));
	}

	[PresentationTestMethod]
	public async Task NthChildMatchControlInPanelWithOffset()
	{
		Border b1, b2, b3, b4;
		var panel = new StackPanel();
		panel.Children.AddRange(new[]
		{
			b1 = new Border(),
			b2 = new Border(),
			b3 = new Border(),
			b4 = new Border()
		});

		var target = default(Selector).NthLastChild(2, 1);

		CornerstoneTest.IsFalse(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b3).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b4).Activator!.Take(1));
	}

	[PresentationTestMethod]
	public async Task NthChildMatchControlInPanelWithPreviousSelector()
	{
		Border b1, b2;
		Button b3, b4;
		var panel = new StackPanel();
		panel.Children.AddRange(new Control[]
		{
			b1 = new Border(),
			b2 = new Border(),
			b3 = new Button(),
			b4 = new Button()
		});

		var previous = default(Selector).OfType<Border>();
		var target = previous.NthLastChild(2, 0);

		CornerstoneTest.IsTrue(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsNull(target.Match(b3).Activator);
		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, target.Match(b3).Result);
		CornerstoneTest.IsNull(target.Match(b4).Activator);
		CornerstoneTest.AreEqual(SelectorMatchResult.NeverThisType, target.Match(b4).Result);
	}

	[PresentationTestMethod]
	public async Task NthChildMatchControlInPanelWithSingularStep()
	{
		Border b1, b2, b3, b4;
		var panel = new StackPanel();
		panel.Children.AddRange(new[]
		{
			b1 = new Border(),
			b2 = new Border(),
			b3 = new Border(),
			b4 = new Border()
		});

		var target = default(Selector).NthLastChild(1, 2);

		CornerstoneTest.IsTrue(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b3).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b4).Activator!.Take(1));
	}

	[PresentationTestMethod]
	public async Task NthChildMatchControlInPanelWithSingularStepWithNegativeOffset()
	{
		Border b1, b2, b3, b4;
		var panel = new StackPanel();
		panel.Children.AddRange(new[]
		{
			b1 = new Border(),
			b2 = new Border(),
			b3 = new Border(),
			b4 = new Border()
		});

		var target = default(Selector).NthLastChild(1, -2);

		CornerstoneTest.IsTrue(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b3).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b4).Activator!.Take(1));
	}

	[PresentationTestMethod]
	public async Task NthChildMatchControlInPanelWithZeroStepWithOffset()
	{
		Border b1, b2, b3, b4;
		var panel = new StackPanel();
		panel.Children.AddRange(new[]
		{
			b1 = new Border(),
			b2 = new Border(),
			b3 = new Border(),
			b4 = new Border()
		});

		var target = default(Selector).NthLastChild(0, 2);

		CornerstoneTest.IsFalse(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b3).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b4).Activator!.Take(1));
	}

	[PresentationTestMethod]
	public void ReturnsCorrectTargetType()
	{
		var target = new NthLastChildSelector(default(Selector).OfType<Control1>(), 1, 0);

		CornerstoneTest.AreEqual(typeof(Control1), target.TargetType);
	}

	#endregion

	#region Classes

	public class Control1 : Control
	{
	}

	#endregion
}