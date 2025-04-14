#region References

using System;
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
		tree.FilterCheck = x => x.Name.Contains("steak", StringComparison.OrdinalIgnoreCase);
		var expected = "{\"Children\":[{\"Children\":[{\"Children\":[],\"Name\":\"Steak Bits\",\"Order\":4}],\"IsParent\":true,\"Name\":\"Appetizers\",\"Order\":1},{\"Children\":[{\"Children\":[],\"Name\":\"New York Strip Steak\",\"Order\":3}],\"IsParent\":true,\"Name\":\"Main Courses\",\"Order\":2}]}";
		AreEqual(expected, tree.ToJson(MinimalSerializeSettings));

		tree.FilterCheck = x => x.Name.Contains("banana", StringComparison.OrdinalIgnoreCase);
		expected = "{\"Children\":[{\"Children\":[{\"Children\":[],\"Name\":\"Banana Bread\",\"Order\":3}],\"IsParent\":true,\"Name\":\"Desserts\",\"Order\":3}]}";
		AreEqual(expected, tree.ToJson(MinimalSerializeSettings));
	}

	[TestMethod]
	public void ToFromJson()
	{
		var tree = GetTreeSample();
		var expected = "{\"Children\":[{\"Children\":[{\"Children\":[],\"Name\":\"Spring Rolls\",\"Order\":1},{\"Children\":[],\"Name\":\"Garlic Bread\",\"Order\":2},{\"Children\":[],\"Name\":\"Cheese Sticks\",\"Order\":3},{\"Children\":[],\"Name\":\"Steak Bits\",\"Order\":4},{\"Children\":[],\"Name\":\"Buffalo Wings\",\"Order\":5},{\"Children\":[],\"Name\":\"Shrimp Cocktail\",\"Order\":6}],\"IsParent\":true,\"Name\":\"Appetizers\",\"Order\":1},{\"Children\":[{\"Children\":[{\"Children\":[],\"Name\":\"Spaghetti\",\"Order\":1},{\"Children\":[],\"Name\":\"Lasagna\",\"Order\":2}],\"IsParent\":true,\"Name\":\"Pasta\",\"Order\":1},{\"Children\":[],\"Name\":\"Grilled Chicken\",\"Order\":2},{\"Children\":[],\"Name\":\"New York Strip Steak\",\"Order\":3},{\"Children\":[],\"Name\":\"Shrimp Scampi\",\"Order\":4}],\"IsParent\":true,\"Name\":\"Main Courses\",\"Order\":2},{\"Children\":[{\"Children\":[],\"Name\":\"Cheesecake\",\"Order\":1},{\"Children\":[],\"Name\":\"Ice Cream\",\"Order\":2},{\"Children\":[],\"Name\":\"Banana Bread\",\"Order\":3}],\"IsParent\":true,\"Name\":\"Desserts\",\"Order\":3}]}";
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