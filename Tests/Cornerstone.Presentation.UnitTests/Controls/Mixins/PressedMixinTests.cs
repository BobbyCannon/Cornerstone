#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Mixins;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Mixins;

[TestClass]
public class PressedMixinTests : ScopedTestBase
{
	#region Fields

	private readonly MouseTestHelper _mouse = new();

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void SelectedClassShouldNotInitiallyBeAdded()
	{
		var target = new TestControl();

		CornerstoneTest.Empty(target.Classes);
	}

	[PresentationTestMethod]
	public void SettingIsSelectedShouldAddSelectedClass()
	{
		var target = new TestControl();

		_mouse.Down(target);

		CornerstoneTest.AreEqual(new[] { ":pressed" }, target.Classes);
	}

	#endregion

	#region Classes

	private class TestControl : Control
	{
		#region Constructors

		static TestControl()
		{
			PressedMixin.Attach<TestControl>();
		}

		#endregion
	}

	#endregion
}