#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

internal class TestTemplatedControl : TemplatedControl
{
	#region Properties

	public bool OnTemplateAppliedCalled { get; private set; }

	#endregion

	#region Methods

	public void AddVisualChild(Visual visual)
	{
		VisualChildren.Add(visual);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		OnTemplateAppliedCalled = true;
	}

	#endregion
}