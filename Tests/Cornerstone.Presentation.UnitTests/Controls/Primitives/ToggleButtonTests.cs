#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class ToggleButtonTests : ScopedTestBase
{
	#region Constants

	private const string checkedClass = ":checked";
	private const string indeterminateClass = ":indeterminate";
	private const string uncheckedClass = ":unchecked";

	#endregion

	#region Methods

	[PresentationTestMethod]
	[DataRow(false, uncheckedClass, false)]
	[DataRow(false, uncheckedClass, true)]
	[DataRow(true, checkedClass, false)]
	[DataRow(true, checkedClass, true)]
	[DataRow(null, indeterminateClass, false)]
	[DataRow(null, indeterminateClass, true)]
	public void ToggleButtonHasCorrectClassAccordingToIsChecked(bool? isChecked, string expectedClass, bool isThreeState)
	{
		var toggleButton = new ToggleButton();
		toggleButton.IsThreeState = isThreeState;
		toggleButton.IsChecked = isChecked;

		CornerstoneTest.Contains(toggleButton.Classes, expectedClass);
	}

	[PresentationTestMethod]
	public void ToggleButtonIsCheckedBindsToBool()
	{
		var toggleButton = new ToggleButton();
		var source = new Class1();

		toggleButton.DataContext = source;
		toggleButton.Bind(ToggleButton.IsCheckedProperty, new Binding("Foo"));

		source.Foo = true;
		CornerstoneTest.IsTrue(toggleButton.IsChecked);

		source.Foo = false;
		CornerstoneTest.IsFalse(toggleButton.IsChecked);
	}

	[PresentationTestMethod]
	public void ToggleButtonIsCheckedChangedIsRaisedOnIsCheckedChanges()
	{
		var threeStateButton = new ToggleButton();
		CornerstoneTest.IsFalse(threeStateButton.IsChecked);

		var changeCount = 0;
		threeStateButton.IsCheckedChanged += (_, _) => ++changeCount;

		threeStateButton.IsChecked = true;
		CornerstoneTest.AreEqual(1, changeCount);
		CornerstoneTest.IsTrue(threeStateButton.IsChecked);

		threeStateButton.IsChecked = false;
		CornerstoneTest.AreEqual(2, changeCount);
		CornerstoneTest.IsFalse(threeStateButton.IsChecked);

		threeStateButton.IsChecked = null;
		CornerstoneTest.AreEqual(3, changeCount);
		CornerstoneTest.IsNull(threeStateButton.IsChecked);
	}

	[PresentationTestMethod]
	public void ToggleButtonIsCheckedChangedIsRaisedWhenToggling()
	{
		var threeStateButton = new TestToggleButton { IsThreeState = true };
		CornerstoneTest.IsFalse(threeStateButton.IsChecked);

		var changeCount = 0;
		threeStateButton.IsCheckedChanged += (_, _) => ++changeCount;

		threeStateButton.Toggle();
		CornerstoneTest.AreEqual(1, changeCount);
		CornerstoneTest.IsTrue(threeStateButton.IsChecked);

		threeStateButton.Toggle();
		CornerstoneTest.AreEqual(2, changeCount);
		CornerstoneTest.IsNull(threeStateButton.IsChecked);

		threeStateButton.Toggle();
		CornerstoneTest.AreEqual(3, changeCount);
		CornerstoneTest.IsFalse(threeStateButton.IsChecked);
	}

	[PresentationTestMethod]
	public void ToggleButtonThreeStateCheckedBindsToNullableBool()
	{
		var threeStateButton = new ToggleButton();
		var source = new Class1();

		threeStateButton.DataContext = source;
		threeStateButton.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(Class1.NullableFoo)));

		source.NullableFoo = true;
		CornerstoneTest.IsTrue(threeStateButton.IsChecked);

		source.NullableFoo = false;
		CornerstoneTest.IsFalse(threeStateButton.IsChecked);

		source.NullableFoo = null;
		CornerstoneTest.IsNull(threeStateButton.IsChecked);
	}

	#endregion

	#region Classes

	private class Class1 : NotifyingBase
	{
		#region Fields

		private bool _foo;
		private bool? nullableFoo;

		#endregion

		#region Properties

		public bool Foo
		{
			get => _foo;
			set
			{
				_foo = value;
				RaisePropertyChanged();
			}
		}

		public bool? NullableFoo
		{
			get => nullableFoo;
			set
			{
				nullableFoo = value;
				RaisePropertyChanged();
			}
		}

		#endregion
	}

	private class TestToggleButton : ToggleButton
	{
		#region Methods

		public new void Toggle()
		{
			base.Toggle();
		}

		#endregion
	}

	#endregion
}