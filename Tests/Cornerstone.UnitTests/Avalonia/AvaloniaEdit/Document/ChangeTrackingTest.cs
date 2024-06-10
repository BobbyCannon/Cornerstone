#region References

using System.Linq;
using Cornerstone.Text.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Document;

[TestClass]
public class ChangeTrackingTest : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void BackwardChanges()
	{
		var document = new TextDocument("initial text");
		var snapshot1 = document.CreateSnapshot();
		document.Replace(0, 7, "nw");
		document.Insert(1, "e");
		var snapshot2 = document.CreateSnapshot();
		AreEqual(1, snapshot2.Version.CompareAge(snapshot1.Version));
		var arr = snapshot2.Version.GetChangesTo(snapshot1.Version).ToArray();
		AreEqual(2, arr.Length);
		AreEqual("", arr[0].InsertedText.Text);
		AreEqual("initial", arr[1].InsertedText.Text);
		AreEqual("initial text", snapshot1.Text);
		AreEqual("new text", snapshot2.Text);
	}

	[TestMethod]
	public void ForwardChanges()
	{
		var document = new TextDocument("initial text");
		var snapshot1 = document.CreateSnapshot();
		document.Replace(0, 7, "nw");
		document.Insert(1, "e");
		var snapshot2 = document.CreateSnapshot();
		AreEqual(-1, snapshot1.Version.CompareAge(snapshot2.Version));
		var arr = snapshot1.Version.GetChangesTo(snapshot2.Version).ToArray();
		AreEqual(2, arr.Length);
		AreEqual("nw", arr[0].InsertedText.Text);
		AreEqual("e", arr[1].InsertedText.Text);

		AreEqual("initial text", snapshot1.Text);
		AreEqual("new text", snapshot2.Text);
	}

	[TestMethod]
	public void NoChanges()
	{
		var document = new TextDocument("initial text");
		var snapshot1 = document.CreateSnapshot();
		var snapshot2 = document.CreateSnapshot();
		AreEqual(0, snapshot1.Version.CompareAge(snapshot2.Version));
		AreEqual(0, snapshot1.Version.GetChangesTo(snapshot2.Version).Count());
		AreEqual(document.Text, snapshot1.Text);
		AreEqual(document.Text, snapshot2.Text);
	}

	#endregion
}