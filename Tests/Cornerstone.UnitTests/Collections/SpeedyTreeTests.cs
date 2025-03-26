#region References

using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Sample.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

[TestClass]
public class SpeedyTreeTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Filter()
	{
		var tree = GetTreeSample();
		tree.FilterCheck = x => x.Name == "bar 1";
		var expected = "{\"Children\":[{\"Children\":[{\"Children\":[],\"Name\":\"bar 1\",\"Order\":1}],\"Name\":\"foo\",\"Order\":1}]}";
		AreEqual(expected, tree.ToJson(MinimalSerializeSettings));

		tree.FilterCheck = x => x.Name == "foo";
		expected = "{\"Children\":[{\"Children\":[],\"Name\":\"foo\",\"Order\":1}]}";
		AreEqual(expected, tree.ToJson(MinimalSerializeSettings));
		
		tree.FilterCheck = x => x.Name == "Header 1";
		expected = "{\"Children\":[{\"Children\":[],\"Name\":\"foo\",\"Order\":1}]}";
		AreEqual(expected, tree.ToJson(MinimalSerializeSettings));
	}

	[TestMethod]
	public void ToFromJson()
	{
		var tree = GetTreeSample();
		var expected = "{\"Children\":[{\"Children\":[{\"Children\":[],\"Name\":\"bar 1\",\"Order\":1},{\"Children\":[{\"Children\":[],\"Name\":\"Header 1\"}],\"Name\":\"bar 2\",\"Order\":2}],\"Name\":\"foo\",\"Order\":1},{\"Children\":[{\"Children\":[],\"Name\":\"world\",\"Order\":1}],\"Name\":\"hello\",\"Order\":2}]}";
		AreEqual(expected, tree.ToJson(MinimalSerializeSettings));

		var fromJson = expected.FromJson<SpeedyTree<MenuItemData>>();
		AreEqual(tree, fromJson);
	}

	private SpeedyTree<MenuItemData> GetTreeSample()
	{
		var tree = new SpeedyTree<MenuItemData>();
		tree.Load(TabSpeedyTree.GetSampleTreeData());
		return tree;
	}

	#endregion
}