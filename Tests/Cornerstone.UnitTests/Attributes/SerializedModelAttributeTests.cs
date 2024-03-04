#region References

using System.Linq;
using Cornerstone.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Attributes;

[TestClass]
public class ValidateSerializedModelTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void SerializedModelsMembers()
	{
		var types = new[]
			{
				typeof(SerializedModelAttribute).Assembly
			}
			.SelectMany(x => x.GetTypes(), (assembly, type) => new { assembly, type })
			.Where(x => x.type.IsDefined(typeof(SerializedModelAttribute), false))
			.Select(x => x.type)
			.ToList();

		ValidateSerializedModelsMembers(types);
	}

	#endregion
}