#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Sync;
using Cornerstone.Testing;
using Cornerstone.Web.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Sync;

#endregion

namespace Cornerstone.UnitTests.Data;

[TestClass]
public class PagedResultsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CalculatePaginationValues()
	{
		// Should only have 1 page
		var results = new PagedResults<object> { Page = 1, PerPage = 10, TotalCount = 10 };
		var actual = results.CalculatePaginationValues();
		AreEqual(1, actual.start);
		AreEqual(1, actual.end);

		// Should only show first 5 pages
		results = new PagedResults<object> { Page = 1, PerPage = 10, TotalCount = 100 };
		actual = results.CalculatePaginationValues();
		AreEqual(1, actual.start);
		AreEqual(5, actual.end);

		// Should only show last 5 pages
		results = new PagedResults<object> { Page = 10, PerPage = 10, TotalCount = 100 };
		actual = results.CalculatePaginationValues();
		AreEqual(6, actual.start);
		AreEqual(10, actual.end);

		// Should only almost first 5 pages
		results = new PagedResults<object> { Page = 4, PerPage = 10, TotalCount = 100 };
		actual = results.CalculatePaginationValues();
		AreEqual(2, actual.start);
		AreEqual(6, actual.end);

		// Should only almost last 5 pages
		results = new PagedResults<object> { Page = 7, PerPage = 10, TotalCount = 100 };
		actual = results.CalculatePaginationValues();
		AreEqual(5, actual.start);
		AreEqual(9, actual.end);
	}

	[TestMethod]
	public void Constructor()
	{
		var request = new PagedRequest();
		var collection = new[] { new AddressSync(), new AddressSync() };
		var actual = new PagedResults<AddressSync>(request, collection.Length, collection);
		AreEqual(2, actual.Results.ToList().Count);
	}

	[TestMethod]
	public void DefaultEmptyJson()
	{
		var request = new PagedRequest();
		var response = new PagedResults<object>(request, 0);
		var actual = response.ToJson();
		actual.Escape().Dump();
		var expected = "{\"Filter\":\"\",\"HasMore\":false,\"Order\":\"\",\"Page\":1,\"PerPage\":10,\"Results\":[],\"TotalCount\":0,\"TotalPages\":1}";
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void HasMore()
	{
		var actual = new PagedResults<object> { Page = 1, PerPage = 10, TotalCount = 10 };
		AreEqual(false, actual.HasMore);

		actual = new PagedResults<object> { Page = 1, PerPage = 10, TotalCount = 11 };
		AreEqual(true, actual.HasMore);

		actual = new PagedResults<object> { Page = 10, PerPage = 1, TotalCount = 11 };
		AreEqual(true, actual.HasMore);

		// This is not a valid result but should still "not" have more data
		actual = new PagedResults<object> { Page = 12, PerPage = 1, TotalCount = 11 };
		AreEqual(false, actual.HasMore);
	}

	[TestMethod]
	public void ResultsFromPagedRequest()
	{
		var request = new PagedRequest();
		request.ParseQueryString("?filter=foo&page=2&roleId=5");
		var actual = new PagedResults<object>(request, 400, 1, "bar");

		AreEqual(2, actual.Page);
		AreEqual(10, actual.PerPage);
		AreEqual(40, actual.TotalPages);
		AreEqual(400, actual.TotalCount);

		AreEqual(new[]
			{
				new PartialUpdateValue("Filter", "foo"),
				new PartialUpdateValue("Page", 2),
				new PartialUpdateValue("roleId", "5"),
				new PartialUpdateValue("TotalCount", 400)
			},
			actual.GetUpdates()
		);
	}

	[TestMethod]
	public void ToJsonThenFromJson()
	{
		var request = new PagedRequest { Page = 2, PerPage = 11 };
		var results = new PagedResults<object>(request, 12, 1, "foo", true);
		var actual = results.ToRawJson();
		var expected = "{\"Filter\":\"\",\"HasMore\":false,\"Order\":\"\",\"Page\":2,\"PerPage\":11,\"Results\":[1,\"foo\",true],\"TotalCount\":12,\"TotalPages\":2}";
		actual.Escape().Dump();
		AreEqual(expected, actual);

		// Options and Updates are not required to be equal after serialization
		var update = expected.FromJson<PagedResults<object>>();
		AreEqual(results, update);

		actual = results.ToRawJson();
		expected = "{\"Filter\":\"\",\"HasMore\":false,\"Order\":\"\",\"Page\":2,\"PerPage\":11,\"Results\":[1,\"foo\",true],\"TotalCount\":12,\"TotalPages\":2}";
		actual.Escape().Dump();
		AreEqual(expected, actual);

		// Options and Updates are not required to be equal after serialization
		update = expected.FromJson<PagedResults<object>>();
		AreEqual(results, update);
	}

	[TestMethod]
	public void ToJsonThenFromJsonSyncObject()
	{
		var request = new PagedRequest { Page = 2, PerPage = 11 };
		var results = new PagedResults<SyncObject>(request, 12,
			GetAccountSync(x =>
			{
				x.Name = "John";
				x.AddressSyncId = Guid.Parse("4562A619-89FA-4D64-A91A-66750184491F");
				x.SyncId = Guid.Parse("E778E441-4486-478B-8661-4AA107FC92E3");
				x.CreatedOn = new DateTime(2022, 10, 17, 05, 43, 21, DateTimeKind.Utc);
				x.ModifiedOn = new DateTime(2022, 10, 17, 05, 43, 22, DateTimeKind.Utc);
			}).ToSyncObject(),
			GetAccountSync(x =>
			{
				x.Name = "Fred";
				x.AddressSyncId = Guid.Parse("E49366C5-0CDB-45F7-A997-8F27471B8FBE");
				x.SyncId = Guid.Parse("F77EC788-38F5-493D-9CA9-EC57BFCDCF8C");
				x.CreatedOn = new DateTime(2022, 10, 17, 05, 43, 23, DateTimeKind.Utc);
				x.ModifiedOn = new DateTime(2022, 10, 17, 05, 43, 24, DateTimeKind.Utc);
			}).ToSyncObject()
		);
		var actual = results.ToRawJson();
		var expected = "{\"Filter\":\"\",\"HasMore\":false,\"Order\":\"\",\"Page\":2,\"PerPage\":11,\"Results\":[{\"Data\":\"{\\\"AddressSyncId\\\":\\\"4562a619-89fa-4d64-a91a-66750184491f\\\",\\\"CreatedOn\\\":\\\"2022-10-17T05:43:21Z\\\",\\\"EmailAddress\\\":\\\"john.doe@domain.com\\\",\\\"ModifiedOn\\\":\\\"2022-10-17T05:43:22Z\\\",\\\"Name\\\":\\\"John\\\",\\\"Roles\\\":\\\",,\\\",\\\"SyncId\\\":\\\"e778e441-4486-478b-8661-4aa107fc92e3\\\"}\",\"ModifiedOn\":\"2022-10-17T05:43:22Z\",\"Status\":1,\"SyncId\":\"e778e441-4486-478b-8661-4aa107fc92e3\",\"TypeName\":\"Sample.Shared.Sync.AccountSync,Sample.Shared\"},{\"Data\":\"{\\\"AddressSyncId\\\":\\\"e49366c5-0cdb-45f7-a997-8f27471b8fbe\\\",\\\"CreatedOn\\\":\\\"2022-10-17T05:43:23Z\\\",\\\"EmailAddress\\\":\\\"john.doe@domain.com\\\",\\\"ModifiedOn\\\":\\\"2022-10-17T05:43:24Z\\\",\\\"Name\\\":\\\"Fred\\\",\\\"Roles\\\":\\\",,\\\",\\\"SyncId\\\":\\\"f77ec788-38f5-493d-9ca9-ec57bfcdcf8c\\\"}\",\"ModifiedOn\":\"2022-10-17T05:43:24Z\",\"Status\":1,\"SyncId\":\"f77ec788-38f5-493d-9ca9-ec57bfcdcf8c\",\"TypeName\":\"Sample.Shared.Sync.AccountSync,Sample.Shared\"}],\"TotalCount\":12,\"TotalPages\":2}";
		//actual.Escape().Dump();
		AreEqual(expected, actual);

		var actualPagedResults = actual.FromJson<PagedResults<SyncObject>>();
		AreEqual(results, actualPagedResults);
		AreEqual(2, actualPagedResults.Results.Count);
	}

	[TestMethod]
	public void ToJsonThenFromJsonWithArrayOfArrays()
	{
		var request = new PagedRequest { Page = 2, PerPage = 11 };
		var results = new PagedResults<int[][]>(request, 12,
			[[1, 2, 3],[4, 5, 6]], [[7, 8, 9],[10, 11, 12]]
		);

		var actual = results.ToRawJson();
		var expected = "{\"Filter\":\"\",\"HasMore\":false,\"Order\":\"\",\"Page\":2,\"PerPage\":11,\"Results\":[[[1,2,3],[4,5,6]],[[7,8,9],[10,11,12]]],\"TotalCount\":12,\"TotalPages\":2}";
		//actual.Escape().Dump();
		AreEqual(expected, actual);

		var actualPagedResults = actual.FromJson<PagedResults<int[][]>>();
		AreEqual(results, actualPagedResults);
		AreEqual(2, actualPagedResults.Results.Count);
	}

	[TestMethod]
	public void TotalPages()
	{
		var actual = new PagedResults<object> { Page = 1, PerPage = 10, TotalCount = 10 };
		AreEqual(1, actual.TotalPages);

		actual = new PagedResults<object> { Page = 1, PerPage = 10, TotalCount = 11 };
		AreEqual(2, actual.TotalPages);

		actual = new PagedResults<object> { Page = 10, PerPage = 1, TotalCount = 10 };
		AreEqual(10, actual.TotalPages);

		actual = new PagedResults<object> { Page = 12, PerPage = 1, TotalCount = 10 };
		AreEqual(10, actual.TotalPages);
	}

	[TestMethod]
	public void UpdateWith()
	{
		var results = new PagedResults<Version>
		{
			Results = new List<Version>
			{
				new(1, 2, 3, 4),
				new(5, 6, 7, 8)
			},
			TotalCount = 4,
			Filter = "Filter",
			Page = 3,
			Order = "Order",
			PerPage = 2
		};

		var actual = new PagedResults<Version>();
		actual.UpdateWith(results);

		AreEqual(results, actual);
		AreEqual("Filter", actual.Filter);
		AreEqual("Order", actual.Order);
		AreEqual(3, actual.Page);
		AreEqual(2, actual.PerPage);
		AreEqual(4, actual.TotalCount);
		AreEqual(new Version(1, 2, 3, 4), actual.Results[0]);
		AreEqual(new Version(5, 6, 7, 8), actual.Results[1]);
	}

	[TestMethod]
	public void VersionsResults()
	{
		var results = new PagedResults<Version>
		{
			Results = new List<Version>
			{
				new(1, 2, 3, 4),
				new(5, 6, 7, 8),
				new(9, 10, 11),
				new(12, 13)
			},
			TotalCount = 4
		};

		var actual = results.ToJson();
		//actual.Escape().CopyToClipboard().Dump();

		#if (NET48)
		var expected = "{\"$id\":\"1\",\"Filter\":\"\",\"HasMore\":false,\"Order\":\"\",\"Page\":1,\"PerPage\":10,\"TotalCount\":4,\"TotalPages\":1,\"Results\":[{\"$id\":\"2\",\"Build\":3,\"Major\":1,\"MajorRevision\":0,\"Minor\":2,\"MinorRevision\":4,\"Revision\":4},{\"$id\":\"3\",\"Build\":7,\"Major\":5,\"MajorRevision\":0,\"Minor\":6,\"MinorRevision\":8,\"Revision\":8},{\"$id\":\"4\",\"Build\":11,\"Major\":9,\"MajorRevision\":-1,\"Minor\":10,\"MinorRevision\":-1,\"Revision\":-1},{\"$id\":\"5\",\"Build\":-1,\"Major\":12,\"MajorRevision\":-1,\"Minor\":13,\"MinorRevision\":-1,\"Revision\":-1}]}";
		#else
		var expected = "{\"Filter\":\"\",\"HasMore\":false,\"Order\":\"\",\"Page\":1,\"PerPage\":10,\"Results\":[\"1.2.3.4\",\"5.6.7.8\",\"9.10.11\",\"12.13\"],\"TotalCount\":4,\"TotalPages\":1}";
		#endif

		AreEqual(expected, actual);
	}

	#endregion

	#region Classes

	public class Test
	{
		#region Properties

		public List<SyncObject> Results { get; set; }

		#endregion
	}

	#endregion
}