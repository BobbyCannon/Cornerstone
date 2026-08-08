#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents a paged request to a service.
/// </summary>
[SourceReflection]
public class PagedRequest : PartialUpdate<PagedRequest>, IPagedRequest
{
	#region Constructors

	/// <summary>
	/// Initializes a paged request to a service.
	/// </summary>
	public PagedRequest() : this([])
	{
	}

	/// <summary>
	/// Initializes a paged request to a service.
	/// </summary>
	/// <param name="values"> A set of values to set. </param>
	public PagedRequest(Dictionary<string, object> values)
	{
		values.ForEach(x => Set(x.Key, x.Value));
	}

	#endregion

	#region Properties

	/// <summary>
	/// An optional filter value.
	/// </summary>
	public string Filter
	{
		get => GetProperty(string.Empty);
		set => SetProperty(value);
	}

	/// <inheritdoc />
	public string Order
	{
		get => GetProperty(string.Empty);
		set => SetProperty(value);
	}

	/// <summary>
	/// The page to start the request on.
	/// </summary>
	public int Page
	{
		get => GetProperty(PageDefault);
		set => SetProperty(value);
	}

	/// <summary>
	/// The number of items per page.
	/// </summary>
	public int PerPage
	{
		get => GetProperty(PerPageDefault);
		set => SetProperty(value);
	}

	/// <summary>
	/// Default value for Page.
	/// </summary>
	protected virtual int PageDefault => 1;

	/// <summary>
	/// Default value for PerPage.
	/// </summary>
	protected virtual int PerPageDefault => 10;

	/// <summary>
	/// Default value for PerPage maximum value.
	/// </summary>
	protected virtual int PerPageMaxDefault => 1000;

	#endregion

	#region Methods

	/// <summary>
	/// Cleanup the request. Set default values.
	/// </summary>
	public virtual PagedRequest Cleanup()
	{
		Cleanup(Filter, x => x == null, () => Filter = string.Empty);
		Cleanup(Order, x => x == null, () => Order = string.Empty);
		Cleanup(Page, x => x <= 0, () => Page = PageDefault);
		Cleanup(PerPage, x => x <= 0, () => PerPage = PerPageDefault);
		Cleanup(PerPage, x => x > PerPageMaxDefault, () => PerPage = PerPageMaxDefault);
		return this;
	}

	/// <summary>
	/// True if the partial update contains an update value.
	/// </summary>
	/// <param name="name"> The update name. </param>
	/// <returns> True if the update is available otherwise false. </returns>
	public bool ContainsUpdate(string name)
	{
		return Updates.ContainsKey(name);
	}

	/// <summary>
	/// Parse the query string into the partial update.
	/// </summary>
	/// <param name="queryString"> The query string to process. </param>
	/// <remarks>
	/// see https://www.ietf.org/rfc/rfc2396.txt for details on url decoding
	/// </remarks>
	public void ParseQueryString(string queryString)
	{
		var collection = HttpUtility.ParseQueryString(queryString);
		var properties = SourceReflector
			.GetRequiredSourceType(GetType())
			.GetProperties()
			.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

		foreach (var key in collection.AllKeys)
		{
			if (key == null)
			{
				continue;
			}

			if (properties.TryGetValue(key, out var property))
			{
				if (collection.Get(key).TryConvertTo(property.PropertyInfo.PropertyType, out var result))
				{
					Set(property.Name, property.PropertyInfo.PropertyType, result);
					continue;
				}
			}

			if (key.EndsWith("[]"))
			{
				var newKey = key.Substring(0, key.Length - 2);
				var newValue = collection.Get(key)?.Split(',');
				Set(newKey, typeof(string[]), newValue);
				continue;
			}

			var value = collection.Get(key);
			Set(key, typeof(string), value);
		}
	}

	protected internal override void RefreshUpdates()
	{
		Set(nameof(Filter), Filter);
		Set(nameof(Order), Order);
		Set(nameof(Page), Page);
		Set(nameof(PerPage), PerPage);
		base.RefreshUpdates();
	}

	/// <summary>
	/// Cleanup a single item based on the test.
	/// </summary>
	/// <typeparam name="T"> The item type to be cleaned up. </typeparam>
	/// <param name="item"> The item to test and clean up. </param>
	/// <param name="test"> The test for the time. </param>
	/// <param name="action"> The action to clean up the item. </param>
	private static void Cleanup<T>(T item, Func<T, bool> test, Action action)
	{
		if (test(item))
		{
			action();
		}
	}

	#endregion
}

/// <summary>
/// Represents a request for paged results from a service.
/// </summary>
public interface IPagedRequest
{
	#region Properties

	/// <summary>
	/// The filter to limit the request to. Defaults to an empty filter.
	/// </summary>
	string Filter { get; set; }

	/// <summary>
	/// The value to order the request by.
	/// </summary>
	public string Order { get; set; }

	/// <summary>
	/// The page to start the request on.
	/// </summary>
	int Page { get; set; }

	/// <summary>
	/// The number of items per page.
	/// </summary>
	int PerPage { get; set; }

	#endregion
}