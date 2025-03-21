#region References

using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

[TestClass]
public class SpeedyTreeTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Name()
	{
		var tree = new SpeedyTree<MenuItemData2>(null, null)
		{
			new()
			{
				Name = "One",
				Order = 1,
				Children =
				{
					new MenuItemData2 { Name = "Hello"}
				}
			},
			new() { Name = "Two", Order = 2 }
		};

		tree.DumpJson();
	}

	#endregion

	#region Classes

	public class MenuItemData2 : SpeedyTree<MenuItemData2>
	{
		#region Constructors

		public MenuItemData2() : this(null)
		{
		}

		public MenuItemData2(IDispatcher dispatcher) : base(null, dispatcher)
		{
		}

		#endregion

		#region Properties

		public ISpeedyList<MenuItemData2> Children => GetChildren();

		public string Name { get; set; }

		public int Order { get; set; }

		#endregion
	}

	#endregion
}