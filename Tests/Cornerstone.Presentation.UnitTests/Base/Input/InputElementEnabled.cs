#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class InputElementEnabled
{
	#region Methods

	[PresentationTestMethod]
	public void DisabledPseudoclassFollowsIsEffectivelyEnabled()
	{
		Decorator child;
		var target = new Decorator
		{
			Child = child = new Decorator()
		};

		CornerstoneTest.DoesNotContain(child.Classes, ":disabled");

		target.IsEnabled = false;

		CornerstoneTest.Contains(child.Classes, ":disabled");
	}

	[PresentationTestMethod]
	public void IsEffectivelyEnabledFollowsAncestorIsEnabled()
	{
		Decorator child;
		Decorator grandchild;
		var target = new Decorator
		{
			Child = child = new Decorator
			{
				Child = grandchild = new Decorator()
			}
		};

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsTrue(target.IsEffectivelyEnabled);
		CornerstoneTest.IsTrue(child.IsEnabled);
		CornerstoneTest.IsTrue(child.IsEffectivelyEnabled);
		CornerstoneTest.IsTrue(grandchild.IsEnabled);
		CornerstoneTest.IsTrue(grandchild.IsEffectivelyEnabled);

		target.IsEnabled = false;

		CornerstoneTest.IsFalse(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
		CornerstoneTest.IsTrue(child.IsEnabled);
		CornerstoneTest.IsFalse(child.IsEffectivelyEnabled);
		CornerstoneTest.IsTrue(grandchild.IsEnabled);
		CornerstoneTest.IsFalse(grandchild.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void IsEffectivelyEnabledFollowsIsEnabled()
	{
		var target = new Decorator();

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsTrue(target.IsEffectivelyEnabled);

		target.IsEnabled = false;

		CornerstoneTest.IsFalse(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void IsEffectivelyEnabledRespectsIsEnabledCore()
	{
		Decorator child;
		var target = new TestControl
		{
			Child = child = new Decorator()
		};

		target.ShouldEnable = false;

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
		CornerstoneTest.IsTrue(child.IsEnabled);
		CornerstoneTest.IsFalse(child.IsEffectivelyEnabled);
	}

	#endregion

	#region Classes

	private class TestControl : Decorator
	{
		#region Fields

		private bool _shouldEnable;

		#endregion

		#region Properties

		public bool ShouldEnable
		{
			get => _shouldEnable;
			set
			{
				_shouldEnable = value;
				UpdateIsEffectivelyEnabled();
			}
		}

		protected override bool IsEnabledCore => IsEnabled && _shouldEnable;

		#endregion
	}

	#endregion
}