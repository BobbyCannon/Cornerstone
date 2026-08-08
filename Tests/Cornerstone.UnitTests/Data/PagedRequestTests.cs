#region References

using Cornerstone.Data;
using Cornerstone.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class PagedRequestTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToFromJson()
	{
		var request = new PagedRequest();
		request.Set("folderId", 150);
		IsTrue(request.TryGet("FolderId", out int folderId));
		AreEqual(150, folderId);

		var expected = "{\"folderId\":150,\"filter\":\"\",\"order\":\"\",\"page\":1,\"perPage\":10}";
		AreEqual(expected, Serializer.ToJson(request));

		var actual = expected.FromJson<PagedRequest>();
		AreEqual(request, actual);
	}

	#endregion
}