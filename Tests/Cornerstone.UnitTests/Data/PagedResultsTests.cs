#region References

using Cornerstone.Data;
using Cornerstone.Sample.Models;
using Cornerstone.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class PagedResultsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToFromJson()
	{
		var request = new PagedResults<Account>();
		request.Set("folderId", 150);
		IsTrue(request.TryGet("FolderId", out int folderId));
		AreEqual(150, folderId);

		var expected = "{\"filter\":\"\",\"order\":\"\",\"page\":1,\"perPage\":10,\"totalCount\":0,\"folderId\":150,\"results\":[],\"totalPages\":1,\"hasMore\":false}";
		AreEqual(expected, Serializer.ToJson(request));

		var actual = expected.FromJson<PagedResults<Account>>();
		AreEqual(request, actual);
	}

	#endregion
}