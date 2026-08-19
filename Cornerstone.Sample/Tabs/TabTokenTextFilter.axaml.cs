#region References

using Cornerstone.Avalonia;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Search;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabTokenTextFilter : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Token Text Filter";

	#endregion

	#region Constructors

	public TabTokenTextFilter()
	{
		FilterText = string.Empty;
		Items = new PresentationList<TokenTextFilterSampleItem>
		{
			FilterCheck = MatchesFilter
		};
		Items.Add(new TokenTextFilterSampleItem("Honeycrisp Apples", "Produce", "for pie"));
		Items.Add(new TokenTextFilterSampleItem("Whole Wheat Bread", "Bakery", "sandwich loaf"));
		Items.Add(new TokenTextFilterSampleItem("Oat Milk", "Dairy", "barista blend"));
		Items.Add(new TokenTextFilterSampleItem("Sharp Cheddar", "Dairy", "block"));
		Items.Add(new TokenTextFilterSampleItem("Sparkling Water", "Beverages", "lime"));

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	[Notify]
	public partial string FilterText { get; set; }

	public PresentationList<TokenTextFilterSampleItem> Items { get; }

	#endregion

	#region Methods

	protected override void OnPropertyChanged(string propertyName)
	{
		if ((propertyName == nameof(FilterText)) && (Items != null))
		{
			Items?.RefreshFilter();
		}

		base.OnPropertyChanged(propertyName);
	}

	private bool MatchesFilter(TokenTextFilterSampleItem item)
	{
		return TokenTextFilter.Matches(FilterText, item.Name, item.Aisle, item.Note);
	}

	#endregion
}

public class TokenTextFilterSampleItem
{
	#region Constructors

	public TokenTextFilterSampleItem(string name, string aisle, string note)
	{
		Name = name;
		Aisle = aisle;
		Note = note;
	}

	#endregion

	#region Properties

	public string Aisle { get; }

	public string Name { get; }

	public string Note { get; }

	#endregion
}