#region References

using System.Collections.Generic;
using Cornerstone.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class NotifiableTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void OnPropertyChangingAndChanged()
	{
		var changed = new List<string>();
		var changing = new List<string>();
		var test = new TestClass();
		test.PropertyChanged += (sender, args) => changed.Add(args.PropertyName);
		test.PropertyChanging += (sender, args) => changing.Add(args.PropertyName);
		test.Name = "Test";

		AreEqual(new[] { "Name" }, changed);
		
		Assert.Inconclusive("complete test?");
		//AreEqual(new[] { "Name" }, changing);
	}

	#endregion

	public class TestClass : Notifiable
	{
		public string Name { get; set; }
	}
}