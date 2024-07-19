#region References

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Sample.FontAwesome;
using Cornerstone.Avalonia;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Profiling;

#endregion

namespace Avalonia.Sample.Tabs;

public class TabIconsModel : ViewModel
{
	#region Fields

	private readonly DebounceService _debounceFilter;
	private readonly string _fontAwesomePath;
	private bool _isLoading;
	private string _searchFilter;
	private bool _selectedOnlyFilter;
	private readonly string _settingsPath;
	private bool _showOnlyBrandsFilter;

	#endregion

	#region Constructors

	public TabIconsModel()
	{
		_debounceFilter = new DebounceService(TimeSpan.FromMilliseconds(750), RefreshFilter);

		FontAwesome = new FontAwesomeManager();

		// todo: make this better...
		var fontAwesome = @"C:\Workspaces\BecomeEpic\Cornerstone\Samples\Avalonia.Sample\FontAwesome\";
		_fontAwesomePath = Path.Join(fontAwesome, "FontAwesome.json");
		_settingsPath = Path.Join(fontAwesome, "Selected.json");
	}

	#endregion

	#region Properties

	public FontAwesomeManager FontAwesome { get; }

	public bool IsLoading
	{
		get => _isLoading;
		private set => SetProperty(ref _isLoading, value);
	}

	public string SearchFilter
	{
		get => _searchFilter;
		set
		{
			if (SetProperty(ref _searchFilter, value))
			{
				_debounceFilter.Trigger();
			}
		}
	}

	public bool SelectedOnlyFilter
	{
		get => _selectedOnlyFilter;
		set
		{
			if (SetProperty(ref _selectedOnlyFilter, value))
			{
				_debounceFilter.Trigger();
			}
		}
	}

	public bool ShowOnlyBrandsFilter
	{
		get => _showOnlyBrandsFilter;
		set
		{
			if (SetProperty(ref _showOnlyBrandsFilter, value))
			{
				_debounceFilter.Trigger();
			}
		}
	}

	#endregion

	#region Methods

	public void ClearSearchFilter()
	{
		SearchFilter = string.Empty;
	}

	public Task LoadAsync()
	{
		IsLoading = true;

		return Task.Run(async () =>
			{
				await FontAwesome.LoadAsync(_fontAwesomePath);
				FontAwesome.Icons.FilterCheck = IconsFilterCheck;
				RefreshFilter(CancellationToken.None);
			})
			.ContinueWith(_ => { CornerstoneDispatcher.Instance.DispatchAsync(() => IsLoading = false); });
	}

	public void ProcessFontAwesomeGenerator()
	{
		// todo: make this better...
		var output = @"C:\Workspaces\BecomeEpic\Cornerstone\Cornerstone.Avalonia\Themes\Shared\";

		if (FontAwesome.Icons.Count <= 0)
		{
			return;
		}

		Save();
		FontAwesomeProcessor.GenerateAvaloniaStyles(FontAwesome, output);
	}

	public void Save()
	{
		var icons = FontAwesome.IconLookup.Select(x => x.Key).ToArray();
		var json = icons.ToJson();
		File.WriteAllText(_settingsPath, json, Encoding.UTF8);
	}

	private bool IconsFilterCheck(FontAwesomeIcon x)
	{
		return x.IsVisible =
			!SelectedOnlyFilter
			&& (!ShowOnlyBrandsFilter
				|| (ShowOnlyBrandsFilter && (x.Svg.Brands != null)))
			&& (string.IsNullOrWhiteSpace(SearchFilter)
				|| x.Label.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)
				|| x.Search.Terms.Any(t => t.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase))
			);
	}

	private void RefreshFilter(CancellationToken cancellationToken)
	{
		CornerstoneDispatcher
			.Instance
			.DispatchAsync(() =>
			{
				try
				{
					IsLoading = true;
					FontAwesome.Icons.RefreshFilter();
				}
				finally
				{
					IsLoading = false;
				}
			});
	}

	#endregion
}