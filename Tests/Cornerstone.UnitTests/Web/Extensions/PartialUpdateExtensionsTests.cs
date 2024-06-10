#region References

using System;
using Cornerstone.Data;
using Cornerstone.Testing;
using Cornerstone.Web.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Web.Extensions;

[TestClass]
public class PartialUpdateExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void FromQueryString()
	{
		var request = new PagedRequest();
		request.ParseQueryString("?filter=test&page=23");
		AreEqual("test", request.Filter);
		AreEqual(23, request.Page);
		AreEqual("test", request.Get<string>("filter"));
		AreEqual(23, request.Get<int>("page"));

		request.ParseQueryString("?options[]=foo&options[]=bar");
		var actual = request.Get<string[]>(name: "options");
		AreEqual(new[] { "foo", "bar" }, actual);
	}

	[TestMethod]
	public void FromQueryStringMissingValue()
	{
		// used to throw exception with null key
		var request = new PagedRequest();
		request.ParseQueryString("?includeWeb");
	}

	[TestMethod]
	public void ToQueryString()
	{
		// Scenarios are cumulative so expect the next scenario to have the previous state
		(Action<PagedRequest> update, string expected)[] scenarios =
		[
			(x => x.AddOrUpdate("options", new[] { "foo", "bar" }), "options[]=foo&options[]=bar"),
			(x => x.Remove("options"), ""),
			(_ => { }, ""),
			(x => x.Page = 2, "Page=2"),
			(x => x.PerPage = 1, "Page=2&PerPage=1"),
			(x => x.PerPage = 98, "Page=2&PerPage=98"),
			(x => x.AddOrUpdate("foo", "bar"), "foo=bar&Page=2&PerPage=98"),
			(x => x.AddOrUpdate("Foo", "Bar"), "Foo=Bar&Page=2&PerPage=98"),
			(x => x.AddOrUpdate("hello", "world"), "Foo=Bar&hello=world&Page=2&PerPage=98"),
			(x => x.AddOrUpdate("hello", "again"), "Foo=Bar&hello=again&Page=2&PerPage=98"),
			// Remove should not be case-sensitive
			(x => x.Remove("HELLO"), "Foo=Bar&Page=2&PerPage=98"),
			(x => x.Remove("FOO"), "Page=2&PerPage=98"),
			// Should be able to handle special (reserved, delimiters, etc) characters
			(x => x.AddOrUpdate("Filter", ";/?:@&=+$,"), "Filter=%3b%2f%3f%3a%40%26%3d%2b%24%2c&Page=2&PerPage=98"),
			(x => x.AddOrUpdate("Filter", "<>#%\""), "Filter=%3c%3e%23%25%22&Page=2&PerPage=98"),
			(x => x.AddOrUpdate("Filter", "{}|\"^[]`"), "Filter=%7b%7d%7c%22%5e%5b%5d%60&Page=2&PerPage=98"),
			(x => x.Remove("FILTER"), "Page=2&PerPage=98"),
			// Should be able to handle special (reserved, delimiters, etc) characters as key
			(x => x.AddOrUpdate(";/?:@&=+$,<>#%\"{}|\"^[]`", "wow..."), "%3b%2f%3f%3a%40%26%3d%2b%24%2c%3c%3e%23%25%22%7b%7d%7c%22%5e%5b%5d%60=wow...&Page=2&PerPage=98")
		];

		var expected = new PagedRequest();

		foreach (var scenario in scenarios)
		{
			scenario.expected.Dump();
			scenario.update(expected);
			AreEqual(scenario.expected, expected.ToQueryString());

			var actual = new PagedRequest();
			actual.ParseQueryString(scenario.expected);

			AreEqual(expected, actual);
		}
	}

	#endregion
}