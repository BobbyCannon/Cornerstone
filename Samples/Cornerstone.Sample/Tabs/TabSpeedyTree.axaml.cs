#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Cornerstone.Avalonia;
using Cornerstone.Extensions;
using Cornerstone.FileSystem;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

public partial class TabSpeedyTree : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Speedy Tree";

	#endregion

	#region Constructors

	public TabSpeedyTree()
	{
		RuntimeInformation = GetInstance<IRuntimeInformation>();
		Dispatcher = GetInstance<IDispatcher>();
		FolderManager = new FolderManager(RuntimeInformation.ApplicationLocation, GetDispatcher())
		{
			FilterCheck = x => string.IsNullOrEmpty(TreeFilter) || (x.Name?.Contains(TreeFilter, StringComparison.OrdinalIgnoreCase) == true)
		};
		Progress = 0;
		SampleTree = new MenuItemData
		{
			FilterCheck = x => string.IsNullOrEmpty(TreeFilter) || (x.Name?.Contains(TreeFilter, StringComparison.OrdinalIgnoreCase) == true)
		};

		// Commands
		ClearCommand = new RelayCommand(_ => Clear());
		SelectFolderCommand = new RelayCommand(_ => SelectFolder());
		RefreshCommand = new RelayCommand(_ => Refresh());

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public bool CancellationPending { get; set; }

	public bool CanSelectFolder => RuntimeInformation.DevicePlatform != DevicePlatform.Browser;

	public ICommand ClearCommand { get; }

	public IDispatcher Dispatcher { get; }

	public FolderManager FolderManager { get; }

	public bool IsRunning { get; set; }

	public string Message { get; set; }

	public int Progress { get; set; }

	public ICommand RefreshCommand { get; }

	public TimeSpan RunElapsed { get; set; }

	public IRuntimeInformation RuntimeInformation { get; }

	public MenuItemData SampleTree { get; }

	public ICommand SelectFolderCommand { get; }

	public string TreeFilter { get; set; }

	#endregion

	#region Methods

	public static IEnumerable<MenuItemData> GetSampleTreeData()
	{
		var appetizers = new MenuItemData { Name = "Appetizers", IsParent = true, Order = 1 };
		appetizers.Add(new MenuItemData { Name = "Spring Rolls", Order = 1 });
		appetizers.Add(new MenuItemData { Name = "Garlic Bread", Order = 2 });
		appetizers.Add(new MenuItemData { Name = "Cheese Sticks", Order = 3 });
		appetizers.Add(new MenuItemData { Name = "Steak Bits", Order = 4 });
		appetizers.Add(new MenuItemData { Name = "Buffalo Wings", Order = 5 });
		appetizers.Add(new MenuItemData { Name = "Shrimp Cocktail", Order = 6 });

		var mainCourses = new MenuItemData { Name = "Main Courses", IsParent = true, Order = 2 };
		var pasta = new MenuItemData { Name = "Pasta", IsParent = true, Order = 1 };
		mainCourses.Add(pasta);
		pasta.Add(new MenuItemData { Name = "Spaghetti", Order = 1 });
		pasta.Add(new MenuItemData { Name = "Lasagna", Order = 2 });
		mainCourses.Add(new MenuItemData { Name = "Grilled Chicken", Order = 2 });
		mainCourses.Add(new MenuItemData { Name = "New York Strip Steak", Order = 3 });
		mainCourses.Add(new MenuItemData { Name = "Shrimp Scampi", Order = 4 });


		var desserts = new MenuItemData { Name = "Desserts", IsParent = true, Order = 3 };
		desserts.Add(new MenuItemData { Name = "Cheesecake", Order = 1 });
		desserts.Add(new MenuItemData { Name = "Ice Cream", Order = 2 });
		desserts.Add(new MenuItemData { Name = "Banana Bread", Order = 3 });

		return [appetizers, mainCourses, desserts];
	}

	/// <inheritdoc />
	public override void OnPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(TreeFilter):
			{
				FolderManager.RefreshFilter();
				SampleTree.RefreshFilter();
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		SampleTree.Load(GetSampleTreeData());
		base.OnLoaded(e);
	}

	private void Clear()
	{
		FolderManager.Clear();
	}

	private void CollapseAllSample(object sender, RoutedEventArgs e)
	{
		SampleTree.ForEachDescendants(x => x.IsExpanded = false);
	}

	private void ExpandAllSamples(object sender, RoutedEventArgs e)
	{
		SampleTree.ForEachDescendants(x => x.IsExpanded = true);
	}

	private void Process()
	{
		IsRunning = true;
		CancellationPending = false;
		Message = "Starting...\r\n";

		var watch = Stopwatch.StartNew();

		try
		{
			FolderManager.Refresh();
		}
		catch (Exception ex)
		{
			Message = ex.ToDetailedString();
		}
		finally
		{
			RunElapsed = watch.Elapsed;
			Message += $"\r\nDone {RunElapsed}";
			IsRunning = false;
		}
	}

	private void Refresh()
	{
		if (IsRunning)
		{
			CancellationPending = true;
			return;
		}

		Task.Run(Process, CancellationToken.None);
	}

	private async void SelectFolder()
	{
		var topLevel = TopLevel.GetTopLevel(this);
		if (topLevel == null)
		{
			return;
		}

		var start = await topLevel.StorageProvider.TryGetFolderFromPathAsync(FolderManager.DirectoryInfo.FullName);
		var options = new FolderPickerOpenOptions
		{
			AllowMultiple = false,
			SuggestedStartLocation = start,
			Title = "Select a folder"
		};
		var dialog = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
		var folder = dialog.FirstOrDefault();

		if (folder != null)
		{
			// Here you can handle the selected folder path
			FolderManager.ChangeDirectory(folder.Path.AbsolutePath, false);
		}
		else
		{
			Console.WriteLine("No folder was selected.");
		}
	}

	private void TreeViewItemOnDoubleTapped(object sender, TappedEventArgs e)
	{
		if (((Visual) e.Source)?.DataContext is DirectoryOrFileInfo info
			&& (info.Children.Count <= 0))
		{
			info.Refresh();
		}
	}

	private void TreeViewItemOnPointerPressed(object sender, PointerPressedEventArgs e)
	{
		if (((Visual) e.Source)?.Parent?.DataContext is DirectoryOrFileInfo info)
		{
			info.Refresh();
		}
	}

	#endregion
}