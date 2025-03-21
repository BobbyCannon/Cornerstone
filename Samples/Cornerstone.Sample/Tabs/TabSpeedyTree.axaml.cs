#region References

using System;
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
		FolderManager = new FolderManager(RuntimeInformation.ApplicationLocation, GetDispatcher());
		Progress = 0;

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

	public ICommand SelectFolderCommand { get; }

	public string TreeFilter { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void OnPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(TreeFilter):
			{
				FolderManager.FilterCheck = string.IsNullOrWhiteSpace(TreeFilter)
					? null
					: x => x.Name?.Contains(TreeFilter) ?? false;
				FolderManager.RefreshFilter();
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		if (Design.IsDesignMode)
		{
		}
		base.OnLoaded(e);
	}

	private void Clear()
	{
		FolderManager.Clear();
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
		if (((Visual) e.Source)?.DataContext is DirectoryOrFileInfo info)
		{
			info.Refresh();
		}
	}

	#endregion
}