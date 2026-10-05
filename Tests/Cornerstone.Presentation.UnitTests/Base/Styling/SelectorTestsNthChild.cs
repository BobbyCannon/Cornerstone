#region References

using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class SelectorTestsNthChild
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(2, 0, ":nth-child(2n)")]
	[DataRow(2, 1, ":nth-child(2n+1)")]
	[DataRow(1, 0, ":nth-child(1n)")]
	[DataRow(4, -1, ":nth-child(4n-1)")]
	[DataRow(0, 1, ":nth-child(1)")]
	[DataRow(0, -1, ":nth-child(-1)")]
	[DataRow(int.MaxValue, int.MinValue + 1, ":nth-child(2147483647n-2147483647)")]
	public void NotSelectorShouldHaveCorrectStringRepresentation(int step, int offset, string expected)
	{
		var target = default(Selector).NthChild(step, offset);

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

		var target = default(Selector).NthChild(0, -2);

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

		var target = default(Selector).NthChild(1, 0);

		CornerstoneTest.AreEqual(SelectorMatch.NeverThisInstance, target.Match(b1));
	}

	[PresentationTestMethod] // http://nthmaster.com/
	[DataRow(+1, 4, -1, 8, false, false, false, true, true, true, true, true, false, false, false)]
	[DataRow(+3, 1, +2, 0, false, false, false, true, false, false, false, false, false, true, false)]
	public async Task NthChildMasterComTestDoubleSelector(
		int step1, int offset1, int step2, int offset2, params bool[] items)
	{
		var panel = new StackPanel();
		panel.Children.AddRange(items.Select(_ => new Border()));

		var previous = default(Selector).OfType<Border>();
		var middle = previous.NthChild(step1, offset1);
		var target = middle.NthChild(step2, offset2);

		var results = new bool[items.Length];
		for (var index = 0; index < items.Length; index++)
		{
			var border = panel.Children[index];
			results[index] = await target.Match(border).Activator!.Take(1);
		}

		CornerstoneTest.AreEqual(items, results);
	}

	[PresentationTestMethod] // http://nthmaster.com/
	[DataRow(+0, 8, false, false, false, false, false, false, false, true, false, false, false)]
	[DataRow(+1, 6, false, false, false, false, false, true, true, true, true, true, true)]
	[DataRow(-1, 9, true, true, true, true, true, true, true, true, true, false, false)]
	public async Task NthChildMasterComTestSigleSelector(
		int step, int offset, params bool[] items)
	{
		var panel = new StackPanel();
		panel.Children.AddRange(items.Select(_ => new Border()));

		var previous = default(Selector).OfType<Border>();
		var target = previous.NthChild(step, offset);

		var results = new bool[items.Length];
		for (var index = 0; index < items.Length; index++)
		{
			var border = panel.Children[index];
			results[index] = await target.Match(border).Activator!.Take(1);
		}

		CornerstoneTest.AreEqual(items, results);
	}

	[PresentationTestMethod] // http://nthmaster.com/
	[DataRow(+1, 2, 2, 1, -1, 9, false, false, true, false, true, false, true, false, true, false, false)]
	public async Task NthChildMasterComTestTripleSelector(
		int step1, int offset1, int step2, int offset2, int step3, int offset3, params bool[] items)
	{
		var panel = new StackPanel();
		panel.Children.AddRange(items.Select(_ => new Border()));

		var previous = default(Selector).OfType<Border>();
		var middle1 = previous.NthChild(step1, offset1);
		var middle2 = middle1.NthChild(step2, offset2);
		var target = middle2.NthChild(step3, offset3);

		var results = new bool[items.Length];
		for (var index = 0; index < items.Length; index++)
		{
			var border = panel.Children[index];
			results[index] = await target.Match(border).Activator!.Take(1);
		}

		CornerstoneTest.AreEqual(items, results);
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

		var target = default(Selector).NthChild(2, 0);

		CornerstoneTest.IsFalse(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b3).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b4).Activator!.Take(1));
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

		var target = default(Selector).NthChild(4, -1);

		CornerstoneTest.IsFalse(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b3).Activator!.Take(1));
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

		var target = default(Selector).NthChild(2, 1);

		CornerstoneTest.IsTrue(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b3).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b4).Activator!.Take(1));
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
		var target = previous.NthChild(2, 0);

		CornerstoneTest.IsFalse(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b2).Activator!.Take(1));
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

		var target = default(Selector).NthChild(1, 2);

		CornerstoneTest.IsFalse(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b3).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b4).Activator!.Take(1));
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

		var target = default(Selector).NthChild(1, -1);

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

		var target = default(Selector).NthChild(0, 2);

		CornerstoneTest.IsFalse(await target.Match(b1).Activator!.Take(1));
		CornerstoneTest.IsTrue(await target.Match(b2).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b3).Activator!.Take(1));
		CornerstoneTest.IsFalse(await target.Match(b4).Activator!.Take(1));
	}

	[PresentationTestMethod]
	public void ReturnsCorrectTargetType()
	{
		var target = new NthChildSelector(default(Selector).OfType<Control1>(), 1, 0);

		CornerstoneTest.AreEqual(typeof(Control1), target.TargetType);
	}

	#endregion

	#region Classes

	public class Control1 : Control
	{
	}

	#endregion
}