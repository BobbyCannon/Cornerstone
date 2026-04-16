#region References

using System.Linq;
using Cornerstone.Reflection;
using Cornerstone.Sample.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Reflection;

[TestClass]
public class SourceTypeInfoTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetPropertyBit()
	{
		var account = new AccountEntity { Name = "Test" };
		IsTrue(account.HasNotifiableChanges(), () => "Should have changes but doesn't.");

		var sourceTypeInfo = SourceReflector.GetSourceType<AccountEntity>();
		var properties = sourceTypeInfo.GetProperties().Select(x => x.Name).ToArray();

		for (var i = 0; i < properties.Length; i++)
		{
			AreEqual(i, sourceTypeInfo.GetPropertyBit(properties[i]));
			AreEqual(properties[i], sourceTypeInfo.GetPropertyNameByBit(i));
		}
	}

	#endregion
}