#region References

using System.Collections.Generic;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

[TestClass]
public class InlineDictionaryTests
{
	#region Methods

	[PresentationTestMethod]
	public void EnumerationAfterAddWithInternalArrayWorks()
	{
		var dic = new InlineDictionary<string, int>();
		dic.Add("foo", 1);
		dic.Add("bar", 2);
		dic.Add("baz", 3);

		CornerstoneTest.AreEqual(new[]
		{
			new KeyValuePair<string, int>("foo", 1),
			new KeyValuePair<string, int>("bar", 2),
			new KeyValuePair<string, int>("baz", 3)
		}, dic);
	}

	[PresentationTestMethod]
	public void EnumerationAfterRemoveWithInternalArrayWorks()
	{
		var dic = new InlineDictionary<string, int>();
		dic.Add("foo", 1);
		dic.Add("bar", 2);
		dic.Add("baz", 3);

		CornerstoneTest.AreEqual(new[]
		{
			new KeyValuePair<string, int>("foo", 1),
			new KeyValuePair<string, int>("bar", 2),
			new KeyValuePair<string, int>("baz", 3)
		}, dic);

		dic.Remove("bar");

		CornerstoneTest.AreEqual(new[]
		{
			new KeyValuePair<string, int>("foo", 1),
			new KeyValuePair<string, int>("baz", 3)
		}, dic);
	}

	[PresentationTestMethod]
	public void SetTwiceWithSingleItemWorks()
	{
		var dic = new InlineDictionary<string, int>();
		dic["foo"] = 1;
		CornerstoneTest.AreEqual(1, dic["foo"]);

		dic["foo"] = 2;
		CornerstoneTest.AreEqual(2, dic["foo"]);
	}

	#endregion
}