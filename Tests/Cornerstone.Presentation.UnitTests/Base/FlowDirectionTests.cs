#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class FlowDirectionTests
{
	#region Methods

	[PresentationTestMethod]
	public void HasMirrorTransformOfChildrenIsUpdatedAfterParentChanged()
	{
		var child = new Visual
		{
			FlowDirection = FlowDirection.LeftToRight
		};

		var target = new Decorator
		{
			FlowDirection = FlowDirection.LeftToRight
		};
		target.VisualChildren.Add(child);

		CornerstoneTest.IsFalse(target.HasMirrorTransform);
		CornerstoneTest.IsFalse(child.HasMirrorTransform);

		target.FlowDirection = FlowDirection.RightToLeft;

		CornerstoneTest.IsTrue(target.HasMirrorTransform);
		CornerstoneTest.IsTrue(child.HasMirrorTransform);
	}

	[PresentationTestMethod]
	public void HasMirrorTransformOfLTRChildrenShouldBeTrueForRTLParent()
	{
		var child = new Visual
		{
			FlowDirection = FlowDirection.LeftToRight
		};

		var target = new Visual
		{
			FlowDirection = FlowDirection.RightToLeft
		};
		target.VisualChildren.Add(child);

		child.InvalidateMirrorTransform();

		CornerstoneTest.IsTrue(target.HasMirrorTransform);
		CornerstoneTest.IsTrue(child.HasMirrorTransform);
	}

	[PresentationTestMethod]
	public void HasMirrorTransformShouldBeTrue()
	{
		var target = new Visual
		{
			FlowDirection = FlowDirection.RightToLeft
		};

		CornerstoneTest.IsTrue(target.HasMirrorTransform);
	}

	#endregion
}