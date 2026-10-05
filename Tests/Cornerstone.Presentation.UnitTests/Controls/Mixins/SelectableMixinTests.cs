#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Mixins;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Mixins;

[TestClass]
public class SelectableMixinTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ClearingIsSelectedShouldRemoveSelectedClass()
	{
		var target = new TestControl();

		target.IsSelected = true;
		target.IsSelected = false;

		CornerstoneTest.Empty(target.Classes);
	}

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

		target.IsSelected = true;

		CornerstoneTest.AreEqual(new[] { ":selected" }, target.Classes);
	}

	[PresentationTestMethod]
	public void SettingIsSelectedShouldRaiseIsSelectedChangedEvent()
	{
		var target = new TestControl();
		var raised = false;

		target.AddHandler(
			SelectingItemsControl.IsSelectedChangedEvent,
			(s, e) => raised = true);

		target.IsSelected = true;

		CornerstoneTest.IsTrue(raised);
	}

	#endregion

	#region Classes

	private class TestControl : Control, ISelectable
	{
		#region Fields

		public static readonly StyledProperty<bool> IsSelectedProperty =
			PresentationProperty.Register<TestControl, bool>(nameof(IsSelected));

		#endregion

		#region Constructors

		static TestControl()
		{
			SelectableMixin.Attach<TestControl>(IsSelectedProperty);
		}

		#endregion

		#region Properties

		public bool IsSelected
		{
			get => GetValue(IsSelectedProperty);
			set => SetValue(IsSelectedProperty, value);
		}

		#endregion
	}

	#endregion
}