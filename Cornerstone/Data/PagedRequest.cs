#region References

using System;
using System.Collections.Generic;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents a paged request to a service.
/// </summary>
public class PagedRequest : PartialUpdate<PagedRequest>, IPagedRequest
{
	#region Constructors

	/// <summary>
	/// Initializes a paged request to a service.
	/// </summary>
	public PagedRequest() : this((IDispatcher) null)
	{
	}

	/// <summary>
	/// Initializes a paged request to a service.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public PagedRequest(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	/// <summary>
	/// Initializes a paged request to a service.
	/// </summary>
	/// <param name="values"> A set of values to set. </param>
	public PagedRequest(Dictionary<string, object> values) : this(values, null)
	{
	}

	/// <summary>
	/// Initializes a paged request to a service.
	/// </summary>
	/// <param name="values"> A set of values to set. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public PagedRequest(Dictionary<string, object> values, IDispatcher dispatcher) : base(dispatcher)
	{
		values.ForEach(x => AddOrUpdate(x.Key, x.Value));
	}

	#endregion

	#region Properties

	/// <summary>
	/// An optional filter value.
	/// </summary>
	public string Filter
	{
		get => Get(string.Empty, nameof(Filter));
		set => Set(value);
	}

	/// <inheritdoc />
	public string Order
	{
		get => Get(string.Empty, nameof(Order));
		set => Set(value);
	}

	/// <summary>
	/// The page to start the request on.
	/// </summary>
	public int Page
	{
		get => Get(PageDefault, nameof(Page));
		set => Set(value);
	}

	/// <summary>
	/// The number of items per page.
	/// </summary>
	public int PerPage
	{
		get => Get(PerPageDefault, nameof(PerPage));
		set => Set(value);
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

	/// <inheritdoc />
	protected internal override void RefreshUpdates()
	{
		AddOrUpdate(nameof(Filter), Filter);
		AddOrUpdate(nameof(Order), Order);
		AddOrUpdate(nameof(Page), Page);
		AddOrUpdate(nameof(PerPage), PerPage);
		base.RefreshUpdates();
	}

	/// <summary>
	/// Cleanup a single item based on the test.
	/// </summary>
	/// <typeparam name="T"> The item type to be cleaned up. </typeparam>
	/// <param name="item"> The item to test and clean up. </param>
	/// <param name="test"> The test for the time. </param>
	/// <param name="action"> The action to cleanup the item. </param>
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