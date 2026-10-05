#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Templates;

[TestClass]
public class TemplateExtensionsTests : ScopedTestBase
{
	#region Methods

	/// <summary>
	/// Control templates can themselves contain templated controls. Make sure that
	/// GetTemplateChildren returns only controls that have a TemplatedParent of the
	/// control that is being searched.
	/// </summary>
	[PresentationTestMethod]
	public void GetTemplateChildrenShouldNotReturnNestedTemplateControls()
	{
		var target = new TestTemplatedControl();
		var border1 = new Border
		{
			Name = "border1",
			TemplatedParent = target
		};
		var inner = new TestTemplatedControl
		{
			Name = "inner",
			TemplatedParent = target
		};
		var border2 = new Border { Name = "border2", TemplatedParent = inner };
		var border3 = new Border { Name = "border3", TemplatedParent = inner };
		var border4 = new Border { Name = "border4", TemplatedParent = target };
		var border5 = new Border { Name = "border5", TemplatedParent = null };

		target.AddVisualChild(border1);
		border1.Child = inner;
		inner.AddVisualChild(border2);
		inner.AddVisualChild(border3);
		border3.Child = border4;
		border4.Child = border5;

		var result = target.GetTemplateDescendants().Select(x => x.Name).ToArray();

		CornerstoneTest.AreEqual(new[] { "border1", "inner", "border4" }, result);
	}

	#endregion
}