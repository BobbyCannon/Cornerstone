#region References

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class GeometryGroupTests
{
	#region Methods

	[PresentationTestMethod]
	public void ChildrenChangeShouldRaiseChanged()
	{
		var target = new GeometryGroup();

		var children = new GeometryCollection();

		target.Children = children;

		var isCalled = false;

		target.Changed += (s, e) => isCalled = true;

		children.Add(new StreamGeometry());

		CornerstoneTest.IsTrue(isCalled);
	}

	[PresentationTestMethod]
	public void ChildrenShouldHaveInitialCollection()
	{
		var target = new GeometryGroup();

		CornerstoneTest.IsNotNull(target.Children);
	}

	#endregion
}