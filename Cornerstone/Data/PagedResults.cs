#region References

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents a page of results for a paged request to a service.
/// </summary>
/// <typeparam name="T"> The type of the items in the results collection. </typeparam>
public class PagedResults<T> : PartialUpdate<PagedResults<T>>, IPagedResults
{
	#region Constructors

	/// <summary>
	/// Instantiate an instance of the paged results.
	/// </summary>
	public PagedResults() : this(new PagedRequest(), 0)
	{
	}

	/// <summary>
	/// Instantiate an instance of the paged results.
	/// </summary>
	/// <param name="request"> The request for the results. </param>
	/// <param name="totalCount"> The total amount of items for the request. </param>
	/// <param name="results"> The items in this set of results. </param>
	public PagedResults(PagedRequest request, int totalCount, params T[] results)
	{
		Initialize(request);

		Results = results.ToList();
		TotalCount = totalCount;

		// Ensure page is not greater than the total pages
		Page = TotalPages < Page ? TotalPages : Page;
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
	public bool HasMore => (Page > 0) && (Page < TotalPages);

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
	/// The results for a paged request.
	/// </summary>
	public IList<T> Results { get; set; }

	/// <inheritdoc />
	public int TotalCount
	{
		get => GetProperty(1);
		set => SetProperty(value);
	}

	/// <inheritdoc />
	public int TotalPages => TotalCount > 0 ? (TotalCount / PerPage) + ((TotalCount % PerPage) > 0 ? 1 : 0) : 1;

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

	/// <inheritdoc />
	public (int start, int end) CalculatePaginationValues()
	{
		var start = Page - 2;
		var end = Page + 2;

		if (start < 1)
		{
			start = 1;
			end = 5;
		}

		if (end > TotalPages)
		{
			end = TotalPages;
			start = end - 4;
		}

		if (start < 1)
		{
			start = 1;
		}

		return (start, end);
	}

	/// <summary>
	/// Convert the results of to a different type.
	/// </summary>
	/// <typeparam name="T2"> The type to convert into. </typeparam>
	/// <param name="convert"> The function to convert from the current type into the requested type. </param>
	/// <returns> The new paged results for the provided type. </returns>
	public PagedResults<T2> ConvertResults<T2>(Func<T, T2> convert)
	{
		var response = new PagedResults<T2>
		{
			Results = Results.Select(convert).ToList(),
			Filter = Filter,
			Order = Order,
			Page = Page,
			PerPage = PerPage,
			TotalCount = TotalCount
		};

		response.Reconcile(this);

		return response;
	}

	protected internal override void RefreshUpdates()
	{
		// Setting values here
		Set(nameof(Filter), Filter);
		Set(nameof(Order), Order);
		Set(nameof(Page), Page);
		Set(nameof(PerPage), PerPage);
		Set(nameof(TotalCount), TotalCount);

		// The results array
		Set(nameof(Results), Results);

		// Calculated properties here
		Set(nameof(TotalPages), TotalPages);
		Set(nameof(HasMore), HasMore);

		base.RefreshUpdates();
	}

	/// <summary>
	/// Update the PagedRequest with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	private void Initialize(PagedRequest update)
	{
		Reconcile(update);

		Filter = update.Filter;
		Order = update.Order;
		Page = update.Page;
		PerPage = update.PerPage;
	}

	#endregion
}

/// <summary>
/// Represents a page of results for a paged request to a service.
/// </summary>
public interface IPagedResults : IPagedRequest
{
	#region Properties

	/// <summary>
	/// The value to determine if the request has more pages.
	/// </summary>
	bool HasMore { get; }

	/// <summary>
	/// The total count of items for the request.
	/// </summary>
	int TotalCount { get; set; }

	/// <summary>
	/// The total count of pages for the request.
	/// </summary>
	int TotalPages { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Calculate the start and end pagination values.
	/// </summary>
	/// <returns> </returns>
	public (int start, int end) CalculatePaginationValues();

	#endregion
}