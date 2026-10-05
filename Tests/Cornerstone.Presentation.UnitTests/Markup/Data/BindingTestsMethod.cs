#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class BindingTestsMethod : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingToPrivateMethodsShouldntWork()
	{
		var vm = new TestClass();
		var target = new Button
		{
			DataContext = vm,
			[!Button.CommandProperty] = new Binding("MyMethod")
		};
		target.RaiseEvent(new AccessKeyEventArgs("b", false));

		CornerstoneTest.IsFalse(vm.IsSet);
	}

	#endregion

	#region Classes

	private class TestClass
	{
		#region Properties

		public bool IsSet { get; set; }

		#endregion

		#region Methods

		private void MyMethod()
		{
			IsSet = true;
		}

		#endregion
	}

	#endregion
}