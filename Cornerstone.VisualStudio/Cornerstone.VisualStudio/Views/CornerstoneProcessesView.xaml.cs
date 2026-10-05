#region References

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Cornerstone.VisualStudio.Services;

#endregion

namespace Cornerstone.VisualStudio.Views;

public partial class CornerstoneProcessesView : UserControl
{
	#region Fields

	private int _activityQueued;

	private readonly ObservableCollection<ProcessStatusRow> _rows;
	private readonly DispatcherTimer _timer;

	#endregion

	#region Constructors

	public CornerstoneProcessesView()
	{
		InitializeComponent();
		_rows = new ObservableCollection<ProcessStatusRow>();
		_activityQueued = 0;
		_timer = new DispatcherTimer();
		_timer.Interval = TimeSpan.FromSeconds(1);
		_timer.Tick += OnTick;
		ProcessList.ItemsSource = _rows;
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	#endregion

	#region Methods

	private void KillProcess(object sender, RoutedEventArgs e)
	{
		var button = sender as Button;
		var row = button?.DataContext as ProcessStatusRow;
		if ((row == null) || !row.CanKill || (row.Kill == null))
		{
			return;
		}

		row.Kill();
		Refresh();
	}

	private void OnActivityChanged()
	{
		// One assembly can report many times. One posted update is enough.
		if (Interlocked.Exchange(ref _activityQueued, 1) == 1)
		{
			return;
		}

		var dispatcher = Dispatcher;
		if ((dispatcher == null) || dispatcher.HasShutdownStarted)
		{
			Interlocked.Exchange(ref _activityQueued, 0);
			return;
		}

		// Background sits below the keyboard. SwitchToMainThreadAsync posts above
		// input, and a metadata load then locks the shell until a resize.
		#pragma warning disable VSTHRD001
		dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ApplyActivityOnUi));
		#pragma warning restore VSTHRD001
	}

	private void ApplyActivityOnUi()
	{
		try
		{
			if (IsLoaded)
			{
				Refresh();
			}
		}
		finally
		{
			Interlocked.Exchange(ref _activityQueued, 0);
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		EditorHostActivity.Changed += OnActivityChanged;
		Refresh();
		_timer.Start();
	}

	private void OnTick(object sender, EventArgs e)
	{
		Refresh();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		EditorHostActivity.Changed -= OnActivityChanged;
		_timer.Stop();
	}

	private void Refresh()
	{
		_rows.Clear();
		CornerstoneStatusBarButton.RefreshToolTip();
		var host = EditorHostSession.DisplayStatus();
		var editor = new ProcessStatusRow
		{
			Kind = "Cornerstone",
			Document = string.Empty,
			State = host.State,
			Activity = host.Activity ?? string.Empty,
			Platform = string.Empty,
			ProcessId = host.ProcessId > 0 ? host.ProcessId.ToString() : string.Empty,
			Detail = host.Detail ?? string.Empty,
			CanKill = host.ProcessId > 0,
			Kill = EditorHostSession.Shutdown
		};
		_rows.Add(editor);

		foreach (var preview in PreviewerProcess.Live())
		{
			var row = new ProcessStatusRow
			{
				Kind = "Preview",
				Document = preview.Document ?? string.Empty,
				Platform = preview.Platform.ToString()
			};
			if (!preview.IsRunning &&
				preview.Status is "Showing" or "Updating" or "Paused")
			{
				row.State = "Closed";
			}
			else if (!string.IsNullOrEmpty(preview.Status))
			{
				row.State = preview.Status;
			}
			else if (!preview.IsRunning)
			{
				row.State = string.IsNullOrEmpty(preview.TargetPath) ? "Idle" : "Stopped";
			}
			else if (preview.IsMarkupPaused)
			{
				row.State = "Paused";
			}
			else if (preview.IsReady)
			{
				row.State = "Showing";
			}
			else
			{
				row.State = "Starting";
			}

			row.Activity = preview.Activity ?? string.Empty;
			row.ProcessId = preview.ProcessId > 0 ? preview.ProcessId.ToString() : string.Empty;
			row.Detail = string.IsNullOrEmpty(preview.TargetPath)
				? string.Empty
				: Path.GetFileName(preview.TargetPath);
			row.CanKill = preview.IsRunning || (preview.ProcessId > 0);
			row.Kill = preview.Kill;
			_rows.Add(row);
		}
	}

	#endregion
}

public sealed class ProcessStatusRow
{
	#region Properties

	public string Activity { get; set; }

	public bool CanKill { get; set; }

	public string Detail { get; set; }

	public string Document { get; set; }

	public Action Kill { get; set; }

	public string Kind { get; set; }

	public string Platform { get; set; }

	public string ProcessId { get; set; }

	public string State { get; set; }

	#endregion
}