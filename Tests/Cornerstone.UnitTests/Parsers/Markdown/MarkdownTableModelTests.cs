#region References

using Cornerstone.Parsers.Markdown;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Markdown;

[TestClass]
public class MarkdownTableModelTests
{
	#region Methods

	[TestMethod]
	public void ParsePreservesLinkSyntaxInCells()
	{
		var table =
			"""
			| Document | Summary |
			|----------|---------|
			| [Keystone.md](Keystone.md) | Bus : State : Engine |
			| [Lifecycle.md](Lifecycle.md#phases) | Lifecycle phases |
			""";

		var model = MarkdownTableModel.Parse(table);
		Assert.IsTrue(model.HasHeader);
		Assert.AreEqual(3, model.Rows.Count);
		Assert.AreEqual(2, model.ColumnCount);
		Assert.AreEqual("Document", model.Rows[0].Cells[0].Source);
		Assert.AreEqual("Summary", model.Rows[0].Cells[1].Source);
		Assert.AreEqual("[Keystone.md](Keystone.md)", model.Rows[1].Cells[0].Source);
		Assert.AreEqual("Bus : State : Engine", model.Rows[1].Cells[1].Source);
		Assert.AreEqual("[Lifecycle.md](Lifecycle.md#phases)", model.Rows[2].Cells[0].Source);
	}

	[TestMethod]
	public void ParseAlignments()
	{
		var table =
			"""
			| L | C | R |
			|:--|:--:|--:|
			| a | b | c |
			""";

		var model = MarkdownTableModel.Parse(table);
		Assert.AreEqual(3, model.Alignments.Count);
		Assert.AreEqual(ColumnAlignment.Left, model.Alignments[0]);
		Assert.AreEqual(ColumnAlignment.Center, model.Alignments[1]);
		Assert.AreEqual(ColumnAlignment.Right, model.Alignments[2]);
	}

	#endregion
}
