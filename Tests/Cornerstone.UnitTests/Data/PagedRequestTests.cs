#region References

using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class PagedRequestTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Defaults()
	{
		var actual = new PagedRequest();
		AreEqual(string.Empty, actual.Filter);
		AreEqual(string.Empty, actual.Order);
		AreEqual(1, actual.Page);
		AreEqual(10, actual.PerPage);
	}

	[TestMethod]
	public void Cleanup()
	{
		var actual = new PagedRequest { Page = 0, PerPage = 1001 };
		AreEqual(0, actual.Page);
		AreEqual(1001, actual.PerPage);

		actual.Cleanup();

		var expected = new PagedRequest { Page = 1, PerPage = 1000 };
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void DefaultEmptyJson()
	{
		var request = new PagedRequest();
		var actual = request.ToJson();
		actual.Escape().Dump();
		var expected = "{\"Filter\":\"\",\"Order\":\"\",\"Page\":1,\"PerPage\":10}";
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void ToJson()
	{
		var request = new PagedRequest();
		var actual = request.ToJson();
		var expected = "{\"Filter\":\"\",\"Order\":\"\",\"Page\":1,\"PerPage\":10}";
		actual.Escape().Dump();
		AreEqual(expected, actual);

		request = new PagedRequest { Page = 2, PerPage = 11 };
		actual = request.ToRawJson();
		expected = "{\"Filter\":\"\",\"Order\":\"\",\"Page\":2,\"PerPage\":11}";
		actual.Escape().Dump();
		AreEqual(expected, actual);

		request.AddOrUpdate("Filter", "frogs");

		actual = request.ToRawJson();
		expected = "{\"Filter\":\"frogs\",\"Order\":\"\",\"Page\":2,\"PerPage\":11}";
		actual.Escape().Dump();
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void UpdateWith()
	{
		var request = new PagedRequest
		{
			Filter = "Filter",
			Order = "Order",
			Page = 2,
			PerPage = 99
		};

		var actual = new PagedRequest();
		actual.UpdateWith(request);

		AreEqual(request, actual);
		AreEqual("Filter", request.Filter);
		AreEqual("Order", request.Order);
		AreEqual(2, request.Page);
		AreEqual(99, request.PerPage);
	}

	#endregion
}