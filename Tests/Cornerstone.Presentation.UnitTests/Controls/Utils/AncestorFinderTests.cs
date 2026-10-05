#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Utils;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Utils;

[TestClass]
public class AncestorFinderTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void SanityCheck()
	{
		var child = new Control();
		var parent = new Decorator();
		var grandParent = new Border();
		var grandParent2 = new Border();

		StyledElement currentParent = null;
		var subscription = AncestorFinder.Create(child, typeof(Border)).Subscribe(s => currentParent = s);

		CornerstoneTest.IsNull(currentParent);
		parent.Child = child;
		CornerstoneTest.IsNull(currentParent);
		grandParent.Child = parent;
		CornerstoneTest.AreEqual(grandParent, currentParent);
		grandParent.Child = null;
		grandParent2.Child = parent;
		CornerstoneTest.AreEqual(grandParent2, currentParent);

		subscription.Dispose();
		parent.Child = null;
		CornerstoneTest.AreEqual(grandParent2, currentParent);
	}

	#endregion
}