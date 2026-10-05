#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

[TestClass]
public class WeakHashListTests
{
	#region Methods

	[PresentationTestMethod]
	public void ArrayCompactAfterRemoveWorks()
	{
		var target = new WeakHashList<string>();

		// Use all slots in array storage.
		var arrMaxSize = WeakHashList<string>.DefaultArraySize;

		for (var i = 0; i < arrMaxSize; i++)
		{
			target.Add(i.ToString());
		}

		// This should compact the array.
		target.Remove("3");

		// And new value should fill empty space.
		target.Add("42");
	}

	[PresentationTestMethod]
	public void IsEmptyWorks()
	{
		var target = new WeakHashList<string>();

		CornerstoneTest.IsTrue(target.IsEmpty);

		target.Add("1");

		CornerstoneTest.IsFalse(target.IsEmpty);

		target.Remove("1");

		CornerstoneTest.IsTrue(target.IsEmpty);

		// Fill array storage.
		var arrMaxSize = WeakHashList<string>.DefaultArraySize;

		for (var i = 0; i < arrMaxSize; i++)
		{
			target.Add(i.ToString());
		}

		CornerstoneTest.IsFalse(target.IsEmpty);

		// This goes above array storage and upgrades to a dictionary.
		target.Add(arrMaxSize.ToString());

		CornerstoneTest.IsFalse(target.IsEmpty);

		// Remove everything, this should still keep an empty dictionary.
		for (var i = 0; i < (arrMaxSize + 1); i++)
		{
			target.Remove(i.ToString());
		}

		CornerstoneTest.IsTrue(target.IsEmpty);
	}

	#endregion
}