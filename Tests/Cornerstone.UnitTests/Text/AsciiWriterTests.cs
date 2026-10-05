#region References

using System.Collections.Generic;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class AsciiWriterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AppendTreeDrawsCornersAndPipes()
	{
		var root = new Node("root",
			new Node("a", new Node("a1"), new Node("a2")),
			new Node("b"));

		var writer = new AsciiWriter();
		writer.AppendTree(root, static n => n.Children, static (n, _) => n.Name);

		AreEqual(
			"root\n├── a\n│   ├── a1\n│   └── a2\n└── b\n",
			writer.ToString().Replace("\r\n", "\n"));
	}

	[TestMethod]
	public void AppendTreeSkipsFilteredLeavesAndKeepsLastCorner()
	{
		var root = new Node("root",
			new Node("keep"),
			new Node("drop"),
			new Node("also"));

		var writer = new AsciiWriter();
		writer.AppendTree(
			root,
			static n => n.Children,
			static (n, _) => n.Name,
			n => n.Name != "drop");

		AreEqual(
			"root\n├── keep\n└── also\n",
			writer.ToString().Replace("\r\n", "\n"));
	}

	[TestMethod]
	public void TreeChildPrefixContinuesOrGaps()
	{
		AreEqual("│   ", AsciiWriter.TreeChildPrefix(string.Empty, false));
		AreEqual("    ", AsciiWriter.TreeChildPrefix(string.Empty, true));
		AreEqual("│   │   ", AsciiWriter.TreeChildPrefix(AsciiWriter.TreeContinue, false));
	}

	#endregion

	#region Classes

	private sealed class Node
	{
		#region Constructors

		public Node(string name, params Node[] children)
		{
			Name = name;
			Children = children;
		}

		#endregion

		#region Properties

		public IReadOnlyList<Node> Children { get; }

		public string Name { get; }

		#endregion
	}

	#endregion
}