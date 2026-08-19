#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.TreeDataGrid;
using Cornerstone.Avalonia.TreeDataGrid.Columns;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Data;
using Cornerstone.Generators;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Search;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// Sample host for TreeDataGrid: hierarchy + flat, with a lab for fixed vs variable row heights (scroll extent).
/// </summary>
[SourceReflection]
public partial class TabTreeDataGrid : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Tree Data Grid";

	#endregion

	#region Fields

	/// <summary>
	/// Every account in the last load (depth-first). Used to clear/apply filters without walking
	/// filtered Children lists (which would skip nodes and leave FilterCheck stuck on).
	/// </summary>
	private List<Account> _allAccountNodes = [];

	private string _filterText = string.Empty;
	private int _rowHeight = 28;
	private bool _useFixedRowHeight = true;
	private bool _useVariableDetailLines;

	#endregion

	#region Constructors

	public TabTreeDataGrid() : this(AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabTreeDataGrid(IRuntimeInformation runtimeInformation)
	{
		RuntimeInformation = runtimeInformation;

		// Hierarchy: root trees only. Flat: every node once (same instances, depth-first).
		Accounts = new PresentationList<Account>();
		FlatAccounts = new PresentationList<Account>();
		TreeDataGridFlatSource = CreateFlatSource();
		TreeDataGridHierarchicalSource = CreateHierarchicalSource();

		if (Design.IsDesignMode && (Accounts.Count == 0))
		{
			Load("tiny");
		}

		DataContext = this;
		InitializeComponent();
		UpdateStatusText();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Root accounts with nested Children (Hierarchy List tab).
	/// </summary>
	public PresentationList<Account> Accounts { get; }

	/// <summary>
	/// Free-text filter applied to both hierarchy and flat lists (Name, Id, Detail).
	/// Hierarchy keeps ancestor chains of matches so hits stay reachable.
	/// </summary>
	public string FilterText
	{
		get => _filterText;
		set
		{
			value ??= string.Empty;
			if (_filterText == value)
			{
				return;
			}

			_filterText = value;
			OnPropertyChanged(nameof(FilterText));
			ApplyAccountFilter();
		}
	}

	/// <summary>
	/// All accounts in depth-first order for the Flat List tab (one row per node).
	/// </summary>
	public PresentationList<Account> FlatAccounts { get; }

	/// <summary>
	/// Bound to both grids' MinRowHeight (fixed slot size when UseFixedRowHeight is true).
	/// </summary>
	public int RowHeight
	{
		get => _rowHeight;
		set
		{
			if (_rowHeight == value)
			{
				return;
			}

			_rowHeight = Math.Clamp(value, 16, 96);
			OnPropertyChanged(nameof(RowHeight));
			UpdateStatusText();
		}
	}

	public IRuntimeInformation RuntimeInformation { get; }

	public string ScrollLabHint =>
		UseFixedRowHeight
			? "Fixed mode: every row is exactly MinRowHeight. Extent = count × height — scrollbar should stay stable. Turn off fixed height (and enable multi-line details) to reproduce estimate thrash."
			: "Variable mode: rows size to content. Extent averages realized heights — thumb can grow/shrink and bottom may jump. Best demo with multi-line details + 50–200 accounts.";

	public string StatusText { get; private set; } = "Load a sample to begin.";

	public FlatTreeDataGridSource<Account> TreeDataGridFlatSource { get; private set; }

	public HierarchicalTreeDataGridSource<Account> TreeDataGridHierarchicalSource { get; private set; }

	/// <summary>
	/// Bound to both grids' UseFixedRowHeight. On = stable scroll; off = content-sized (can thrash).
	/// </summary>
	public bool UseFixedRowHeight
	{
		get => _useFixedRowHeight;
		set
		{
			if (_useFixedRowHeight == value)
			{
				return;
			}

			_useFixedRowHeight = value;
			OnPropertyChanged(nameof(UseFixedRowHeight));
			OnPropertyChanged(nameof(ScrollLabHint));
			UpdateStatusText();
		}
	}

	/// <summary>
	/// When true, generated accounts get multi-line Detail text so variable-height mode is visible.
	/// </summary>
	public bool UseVariableDetailLines
	{
		get => _useVariableDetailLines;
		set
		{
			if (_useVariableDetailLines == value)
			{
				return;
			}

			_useVariableDetailLines = value;
			OnPropertyChanged(nameof(UseVariableDetailLines));

			// Rebuild columns so templates show/hide detail rows; re-apply details on existing data.
			RebuildSources();
			ApplyDetailLinesToAccounts();

			// FilterCheck lives on the lists (not the sources); re-apply after detail changes.
			ApplyAccountFilter();
		}
	}

	#endregion

	#region Methods

	public static void ForEachDescendants(IEnumerable<Account> accounts, Action<Account> action)
	{
		foreach (var account in accounts)
		{
			action(account);
			ForEachDescendants(account.Children, action);
		}
	}

	/// <summary>
	/// Generates random hierarchies that can go deep with variable branching.
	/// </summary>
	[RelayCommand]
	public void Load(object args)
	{
		switch (args?.ToString()?.ToLowerInvariant())
		{
			case "tiny":
				GenerateRandomAccounts(50);
				break;
			case "small":
				GenerateRandomAccounts(200);
				break;
			case "large":
				GenerateRandomAccounts(500_000);
				break;
			case "huge":
				GenerateRandomAccounts(1_000_000);
				break;
			default:
				GenerateRandomAccounts(10_000);
				break;
		}

		// Re-apply filter after reload (lists were rebuilt).
		ApplyAccountFilter();
	}

	/// <summary>
	/// Filter both lists via PresentationList.FilterCheck.
	/// Hierarchy: keep matches and their ancestors; expand matching branches.
	/// Flat: keep only accounts that match the text (no parents, no siblings).
	/// </summary>
	private void ApplyAccountFilter()
	{
		// Always clear using the full-node snapshot (never walk filtered Children).
		ClearAccountFilters();
		RebuildFlatAccountsFromSnapshot();

		var filter = (_filterText ?? string.Empty).Trim();
		if (filter.Length == 0)
		{
			RefreshGridSources(null);
			UpdateStatusText();
			return;
		}

		var parentOf = BuildParentMap(_allAccountNodes);
		var matches = new HashSet<Account>();
		var hierarchyVisible = new HashSet<Account>();

		foreach (var account in _allAccountNodes)
		{
			if (!MatchesFilter(account, filter))
			{
				continue;
			}

			matches.Add(account);
			for (var current = account; current != null; current = parentOf.GetValueOrDefault(current))
			{
				if (!hierarchyVisible.Add(current))
				{
					break;
				}
			}
		}

		bool KeepInHierarchy(Account a)
		{
			return hierarchyVisible.Contains(a);
		}

		bool KeepInFlat(Account a)
		{
			return matches.Contains(a);
		}

		Accounts.FilterCheck = KeepInHierarchy;
		FlatAccounts.FilterCheck = KeepInFlat;
		foreach (var account in _allAccountNodes)
		{
			account.Children.FilterCheck = KeepInHierarchy;
		}

		// Setting FilterCheck already refreshes; call again so child lists are definite.
		Accounts.RefreshFilter();
		FlatAccounts.RefreshFilter();
		foreach (var account in _allAccountNodes)
		{
			account.Children.RefreshFilter();
		}

		RefreshGridSources(hierarchyVisible);
		UpdateStatusText();
	}

	private void ApplyDetailLinesToAccounts()
	{
		foreach (var a in _allAccountNodes)
		{
			a.Detail = UseVariableDetailLines ? BuildDetailLines(a.Id) : string.Empty;
		}
	}

	private static Control BuildAccountCell(Account account)
	{
		// Center the block in the row slot (fixed MinRowHeight is taller than one line of text).
		// A vertical StackPanel alone stays top-aligned within the template cell.
		var panel = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Spacing = 2,
			Margin = new Thickness(4, 2),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};

		var name = new TextBlock
		{
			Text = account?.Name ?? string.Empty
		};
		panel.Children.Add(name);

		var detail = new TextBlock
		{
			Text = account?.Detail ?? string.Empty,
			TextWrapping = TextWrapping.Wrap,
			Opacity = 0.65,
			FontSize = 12,
			IsVisible = !string.IsNullOrWhiteSpace(account?.Detail)
		};

		if (account is not null)
		{
			// Refresh when Detail changes (toggle multi-line / reload) without a custom converter.
			void SyncDetail()
			{
				detail.Text = account.Detail ?? string.Empty;
				detail.IsVisible = !string.IsNullOrWhiteSpace(account.Detail);
			}

			account.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName is null || (e.PropertyName == nameof(Account.Detail)))
				{
					SyncDetail();
				}
			};
		}

		panel.Children.Add(detail);
		return panel;
	}

	private static string BuildDetailLines(int id)
	{
		// Deterministic 0–4 lines so heights vary predictably while scrolling.
		var lineCount = id % 5;
		if (lineCount == 0)
		{
			return string.Empty;
		}

		var builder = new StringBuilder();
		for (var i = 0; i < lineCount; i++)
		{
			if (i > 0)
			{
				builder.AppendLine();
			}

			// Fixed seed material from id so the same row keeps a stable height while scrolling.
			builder.Append("Line ").Append(i + 1).Append(" · id ").Append(id)
				.Append(" · ").Append(RandomGenerator.NextString(12 + ((id + i) % 18)));
		}

		return builder.ToString();
	}

	/// <summary>
	/// Parent map for hierarchy path padding. Caller must clear Children.FilterCheck first.
	/// </summary>
	private static Dictionary<Account, Account> BuildParentMap(List<Account> allNodes)
	{
		var parentOf = new Dictionary<Account, Account>();
		foreach (var account in allNodes)
		{
			foreach (var child in account.Children)
			{
				parentOf[child] = account;
			}
		}

		return parentOf;
	}

	/// <summary>
	/// Remove every FilterCheck using <see cref="_allAccountNodes" /> — do not walk Children
	/// while filtered (that skips nodes and leaves FilterCheck stuck after backspace-clear).
	/// </summary>
	private void ClearAccountFilters()
	{
		foreach (var account in _allAccountNodes)
		{
			account.Children.FilterCheck = null;
		}

		Accounts.FilterCheck = null;
		FlatAccounts.FilterCheck = null;

		Accounts.RefreshFilter();
		FlatAccounts.RefreshFilter();
		foreach (var account in _allAccountNodes)
		{
			account.Children.RefreshFilter();
		}
	}

	private void CollapseAllSample(object sender, RoutedEventArgs e)
	{
		// Collapse via the hierarchical source so the flattened row list rebuilds in one Reset
		// (setting model.IsExpanded alone only updates realized expander cells).
		TreeDataGridHierarchicalSource.CollapseAll();
	}

	/// <summary>
	/// Depth-first collect. Requires Children to be unfiltered (call after ClearAccountFilters
	/// or during load before any filter is applied).
	/// </summary>
	private static void CollectTreeUnfiltered(
		Account account,
		Account parent,
		List<Account> all,
		Dictionary<Account, Account> parents)
	{
		if (account is null)
		{
			return;
		}

		all.Add(account);
		if (parents is not null && parent is not null)
		{
			parents[account] = parent;
		}

		// Intentionally iterates Children; only safe when FilterCheck is null.
		foreach (var child in account.Children)
		{
			CollectTreeUnfiltered(child, account, all, parents);
		}
	}

	/// <summary>
	/// Name (+ optional multi-line Detail) so variable-height mode produces uneven DesiredSize.
	/// </summary>
	private TemplateColumn<Account> CreateAccountColumn()
	{
		return new TemplateColumn<Account>(
			"Account",
			new FuncDataTemplate<Account>((account, _) => BuildAccountCell(account), true),
			null,
			new GridLength(1, GridUnitType.Star));
	}

	private FlatTreeDataGridSource<Account> CreateFlatSource()
	{
		// Must use FlatAccounts — Accounts is only the root set (often much smaller than target count).
		return new FlatTreeDataGridSource<Account>(FlatAccounts)
		{
			Columns =
			{
				CreateAccountColumn(),
				new TextColumn<Account, int>("Children", x => x.Children.Count),
				new TextColumn<Account, int>("Id", x => x.Id)
			}
		};
	}

	private HierarchicalTreeDataGridSource<Account> CreateHierarchicalSource()
	{
		return new HierarchicalTreeDataGridSource<Account>(Accounts)
		{
			Columns =
			{
				new HierarchicalExpanderColumn<Account>(
					CreateAccountColumn(),
					x => x.Children,
					x => x.Children.Count > 0,
					x => x.IsExpanded),
				new TextColumn<Account, int>("Children", x => x.Children.Count),
				new TextColumn<Account, int>("Id", x => x.Id)
			}
		};
	}

	/// <summary>
	/// Recursive helper that creates a single account and its subtree.
	/// </summary>
	private Account CreateRandomAccount(
		int currentDepth,
		int maxDepth,
		ref int accountIdCounter,
		ref int totalCreated,
		int targetCount)
	{
		if (totalCreated >= targetCount)
		{
			return null;
		}

		var id = accountIdCounter++;
		var account = new Account
		{
			Id = id,
			Name = RandomGenerator.GetFullName(),
			IsExpanded = false,
			Detail = UseVariableDetailLines ? BuildDetailLines(id) : string.Empty
		};

		totalCreated++;

		if ((currentDepth >= maxDepth) || (totalCreated >= targetCount))
		{
			return account;
		}

		var numChildren = RandomGenerator.NextInteger(0, 5);
		for (var i = 0; i < numChildren; i++)
		{
			if (totalCreated >= targetCount)
			{
				break;
			}

			var child = CreateRandomAccount(currentDepth + 1, maxDepth, ref accountIdCounter, ref totalCreated, targetCount);
			if (child is not null)
			{
				account.Children.Add(child);
			}
		}

		return account;
	}

	private void ExpandAllSamples(object sender, RoutedEventArgs e)
	{
		// Expand via HierarchicalTreeDataGridSource: walks rows, expands, rebuilds flatten once.
		// That yields count × MinRowHeight extent instead of thrashing per-node inserts.
		TreeDataGridHierarchicalSource.ExpandAll();
	}

	private void GenerateRandomAccounts(int targetCount)
	{
		// Drop any previous FilterCheck so we never snapshot a filtered tree.
		foreach (var account in _allAccountNodes)
		{
			account.Children.FilterCheck = null;
		}

		Accounts.FilterCheck = null;
		FlatAccounts.FilterCheck = null;
		Accounts.Clear();
		FlatAccounts.Clear();
		_allAccountNodes = [];

		if (targetCount <= 0)
		{
			return;
		}

		var accountIdCounter = 1;
		var totalCreated = 0;
		const int maxDepth = 4;

		while (totalCreated < targetCount)
		{
			var root = CreateRandomAccount(0, maxDepth, ref accountIdCounter, ref totalCreated, targetCount);
			if (root is not null)
			{
				Accounts.Add(root);
			}
		}

		// Full-node snapshot before any filter — used for clear/apply and flat list rebuild.
		RebuildAllAccountSnapshot();
		RebuildFlatAccountsFromSnapshot();
	}

	private static bool MatchesFilter(Account account, string filter)
	{
		if (account is null)
		{
			return false;
		}

		return TokenTextFilter.Matches(filter, account.Name, account.Detail, account.Id.ToString());
	}

	/// <summary>
	/// Snapshot every node while Children are unfiltered (at load time).
	/// </summary>
	private void RebuildAllAccountSnapshot()
	{
		_allAccountNodes = [];
		foreach (var root in Accounts)
		{
			CollectTreeUnfiltered(root, null, _allAccountNodes, null);
		}
	}

	/// <summary>
	/// Flat list = every node once, from the unfiltered snapshot (never from filtered Children walks).
	/// </summary>
	private void RebuildFlatAccountsFromSnapshot()
	{
		FlatAccounts.FilterCheck = null;
		FlatAccounts.Load(_allAccountNodes);
	}

	private void RebuildSources()
	{
		TreeDataGridFlatSource = CreateFlatSource();
		TreeDataGridHierarchicalSource = CreateHierarchicalSource();
		OnPropertyChanged(nameof(TreeDataGridFlatSource));
		OnPropertyChanged(nameof(TreeDataGridHierarchicalSource));
	}

	private void RefreshGridSources(HashSet<Account> expandMatching)
	{
		TreeDataGridHierarchicalSource = CreateHierarchicalSource();
		OnPropertyChanged(nameof(TreeDataGridHierarchicalSource));

		TreeDataGridFlatSource = CreateFlatSource();
		OnPropertyChanged(nameof(TreeDataGridFlatSource));

		if (expandMatching is { Count: > 0 } && TreeDataGridHierarchicalSource is not null)
		{
			TreeDataGridHierarchicalSource.ExpandCollapseRecursive(a =>
				expandMatching.Contains(a) && a.Children.Any(c => expandMatching.Contains(c)));
		}
	}

	private void UpdateStatusText()
	{
		var mode = UseFixedRowHeight ? "fixed" : "variable";
		var details = UseVariableDetailLines ? "multi-line details on" : "details off";
		var filter = string.IsNullOrWhiteSpace(FilterText)
			? "filter off"
			: $"filter=\"{FilterText.Trim()}\"";

		// Flat count = direct matches; hierarchy roots may include path-only parents.
		StatusText =
			$"Mode: {mode} · MinRowHeight={RowHeight} · {details} · {filter} · " +
			$"flat matches={FlatAccounts.Count} · hierarchy roots={Accounts.Count}";
		OnPropertyChanged(nameof(StatusText));
	}

	#endregion

	#region Classes

	[Notifiable(["*"])]
	[Updateable(UpdateableAction.All, ["*"])]
	public partial class Account : CornerstoneObject
	{
		#region Constructors

		public Account()
		{
			Children = new PresentationList<Account>();
			Detail = string.Empty;
		}

		#endregion

		#region Properties

		public PresentationList<Account> Children { get; }

		/// <summary>
		/// Optional multi-line body for variable-height scroll demos.
		/// </summary>
		public partial string Detail { get; set; }

		public partial int Id { get; set; }

		public partial bool IsExpanded { get; set; }

		public partial string Name { get; set; }

		#endregion
	}

	#endregion
}