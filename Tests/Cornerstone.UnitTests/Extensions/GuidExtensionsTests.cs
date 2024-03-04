#region References

using System;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class GuidExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void NumberToGuidShouldWork()
	{
		AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000001"), 1.ToGuid());
		AreEqual(Guid.Parse("00000000-0000-0000-0000-000000123456"), 123456.ToGuid());
		AreEqual(Guid.Parse("00000000-0000-0000-0012-345678901234"), 12345678901234L.ToGuid());
		AreEqual(Guid.Parse("00000000-0000-0000-0123-456789012345"), 123456789012345U.ToGuid());
	}

	#endregion
}