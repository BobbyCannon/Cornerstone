#region References

using System;
using Cornerstone.VisualStudio.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class NewGuidFormatTests
{
	#region Methods

	[TestMethod]
	public void CreateUsesOneGuidInEachFormat()
	{
		var guid = new Guid("B53CCB8B-4AEF-4BBE-B308-1F5F3B7F51B3");
		var formats = NewGuidFormats.Create(guid);

		Assert.AreEqual(9, formats.Length);
		Assert.AreEqual("B53CCB8B-4AEF-4BBE-B308-1F5F3B7F51B3", formats[0].Text);
		Assert.AreEqual("Dashes", formats[0].Description);
		Assert.AreEqual("b53ccb8b-4aef-4bbe-b308-1f5f3b7f51b3", formats[1].Text);
		Assert.AreEqual("B53CCB8B4AEF4BBEB3081F5F3B7F51B3", formats[2].Text);
		Assert.AreEqual("b53ccb8b4aef4bbeb3081f5f3b7f51b3", formats[3].Text);
		Assert.AreEqual("{B53CCB8B-4AEF-4BBE-B308-1F5F3B7F51B3}", formats[4].Text);
		Assert.AreEqual("{b53ccb8b-4aef-4bbe-b308-1f5f3b7f51b3}", formats[5].Text);
		Assert.AreEqual("(B53CCB8B-4AEF-4BBE-B308-1F5F3B7F51B3)", formats[6].Text);
		Assert.AreEqual("(b53ccb8b-4aef-4bbe-b308-1f5f3b7f51b3)", formats[7].Text);
		Assert.AreEqual(guid.ToString("X"), formats[8].Text);
	}

	[TestMethod]
	public void TokenMatchesAtCaret()
	{
		int start;
		int length;
		Assert.IsTrue(NewGuidFormats.TryGetTokenSpan("nguid", 5, out start, out length));
		Assert.AreEqual(0, start);
		Assert.AreEqual(5, length);

		Assert.IsTrue(NewGuidFormats.TryGetTokenSpan("id = NGUID", 10, out start, out length));
		Assert.AreEqual(5, start);
		Assert.AreEqual(5, length);
	}

	[TestMethod]
	public void TokenRejectsALongerWord()
	{
		int start;
		int length;
		Assert.IsFalse(NewGuidFormats.TryGetTokenSpan("mynguid", 7, out start, out length));
		Assert.IsFalse(NewGuidFormats.TryGetTokenSpan("nguidX", 5, out start, out length));
		Assert.IsFalse(NewGuidFormats.TryGetTokenSpan("nguid", 3, out start, out length));
		Assert.IsFalse(NewGuidFormats.TryGetTokenSpan("n_guid", 6, out start, out length));
	}

	#endregion
}
