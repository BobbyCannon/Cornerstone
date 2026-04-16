#region References

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.TreeDataGrid;
using Cornerstone.Avalonia.TreeDataGrid.Columns;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Generators;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

[SourceReflection]
public partial class TabTreeDataGrid : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Tree Data Grid";

	#endregion

	#region Constructors

	public TabTreeDataGrid() : this(CornerstoneApplication.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabTreeDataGrid(IRuntimeInformation runtimeInformation)
	{
		RuntimeInformation = runtimeInformation;
		Accounts = new PresentationList<Account>();
		TreeDataGridFlatSource = new FlatTreeDataGridSource<Account>(Accounts)
		{
			Columns =
			{
				new TextColumn<Account, string>("Name", x => x.Name, new GridLength(1, GridUnitType.Star)),
				new TextColumn<Account, int>("Children", x => x.Children.Count),
				new TextColumn<Account, int>("Id", x => x.Id)
			}
		};
		TreeDataGridHierarchicalSource = new HierarchicalTreeDataGridSource<Account>(Accounts)
		{
			Columns =
			{
				new HierarchicalExpanderColumn<Account>(
					new TextColumn<Account, string>("Name", x => x.Name, new GridLength(1, GridUnitType.Star)),
					x => x.Children,
					x => x.Children.Count > 0,
					x => x.IsExpanded),
				new TextColumn<Account, int>("Children", x => x.Children.Count),
				new TextColumn<Account, int>("Id", x => x.Id)
			}
		};

		if (Design.IsDesignMode && (Accounts.Count == 0))
		{
			Load(null);
		}

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public PresentationList<Account> Accounts { get; }

	public IRuntimeInformation RuntimeInformation { get; }

	public FlatTreeDataGridSource<Account> TreeDataGridFlatSource { get; }

	public HierarchicalTreeDataGridSource<Account> TreeDataGridHierarchicalSource { get; set; }

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
	/// Generates random hierarchies that can go deep (max depth 15) with variable branching.
	/// </summary>
	[RelayCommand]
	public void Load(object args)
	{
		switch (args?.ToString()?.ToLowerInvariant())
		{
			case "large":
			{
				GenerateRandomAccounts(500_000);
				break;
			}
			case "huge":
			{
				GenerateRandomAccounts(1_000_000);
				break;
			}
			default:
			{
				GenerateRandomAccounts(10_000);
				break;
			}
		}
	}

	private void CollapseAllSample(object sender, RoutedEventArgs e)
	{
		ForEachDescendants(Accounts, x => x.IsExpanded = false);
	}

	/// <summary>
	/// Recursive helper that creates a single account and its subtree.
	/// Stops creating nodes exactly when the global target count is reached.
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

		var account = new Account
		{
			Id = accountIdCounter++,
			Name = RandomGenerator.GetFullName(),
			IsExpanded = false
		};

		totalCreated++;

		// Leaf node if we hit max depth or the target count
		if ((currentDepth >= maxDepth) || (totalCreated >= targetCount))
		{
			return account;
		}

		// Random number of children (0–4) – creates varied depth and width
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
		ForEachDescendants(Accounts, x => x.IsExpanded = true);
	}

	/// <summary>
	/// Generates the requested number of Account objects with random deep hierarchies.
	/// The tree is built recursively with a maximum depth of 15 and random branching (0-4 children per node).
	/// Total account count will be very close to the target (never exceeds it).
	/// All nodes start collapsed for UI performance.
	/// </summary>
	private void GenerateRandomAccounts(int targetCount)
	{
		Accounts.Clear();

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
	}

	#endregion

	#region Classes

	[Notifiable(["*"])]
	[Updateable(UpdateableAction.All, ["*"])]
	public partial class Account : Notifiable
	{
		#region Constructors

		public Account()
		{
			Children = new PresentationList<Account>();
		}

		#endregion

		#region Properties

		public PresentationList<Account> Children { get; }

		public partial int Id { get; set; }

		public partial bool IsExpanded { get; set; }

		public partial string Name { get; set; }

		#endregion
	}

	#endregion
}