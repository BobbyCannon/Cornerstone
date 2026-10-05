#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Sample.Tabs.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Controls;

/// <summary>
/// Regression: opening the Buttons sample (Press Hold NumericUpDown) threw OverflowException (decimal)
/// from double→decimal bindings (Height/Width are double; NaN when unset; NumericUpDown.Value is decimal).
/// </summary>
[TestClass]
public class TabButtonLoadTests : CornerstoneCornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ConvertToDecimalOfNaNHeightThrowsOverflowException()
	{
		// Reproduce Cornerstone's default coercion when Height is unset (double.NaN).
		// Note: Cornerstone.Convert shadows System.Convert — use global::System.Convert.
		var height = double.NaN;
		var ex = Assert.ThrowsExactly<OverflowException>(() => _ = System.Convert.ToDecimal(height));
		StringAssert.Contains(ex.Message, "too large or too small for a Decimal");
	}

	[TestMethod]
	public void TabButtonConstructAndLayoutDoesNotThrow()
	{
		// RunOnUi uses HeadlessUnitTestSession (live UI thread). Do not use Dispatcher.UIThread.Invoke —
		// that deadlocks when the init thread is not pumping.
		var failure = RunOnUi(() =>
		{
			try
			{
				// Avoid AppBootstrap DI: pass date/time provider from the test host.
				var tab = new TabControlsButtons(this);

				// Force measure/arrange so styles, templates, and bindings apply (same as opening the tab).
				tab.Width = 800;
				tab.Height = 600;
				tab.Measure(new Size(800, 600));
				tab.Arrange(new Rect(0, 0, 800, 600));
				tab.UpdateLayout();

				// Walk visual tree to ensure PressHoldButton templates (PercentWidth multi-bindings) run.
				foreach (var descendant in tab.GetVisualDescendants())
				{
					if (descendant is PressHoldButton button)
					{
						button.Measure(new Size(200, 40));
						button.Arrange(new Rect(0, 0, 120, 32));
						button.UpdateLayout();
					}
				}

				RunUiJobs();
				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		});

		Assert.IsNull(failure, failure?.ToString());
	}

	#endregion
}