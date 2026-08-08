#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.Core.DnlibMetadataProvider;
using Cornerstone.VisualStudio.Core.Parsing;
using Cornerstone.VisualStudio.Models;
using Cornerstone.VisualStudio.Services;
using EnvDTE;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Serilog;
using Task = System.Threading.Tasks.Task;

#endregion

namespace Cornerstone.VisualStudio.Views;

/// <summary>
/// The Avalonia XAML designer control.
/// </summary>
internal partial class AvaloniaDesigner : IDisposable
{
	#region Fields

	public static readonly DependencyProperty SelectedTargetProperty;
	public static readonly DependencyProperty SplitOrientationProperty;
	public static readonly DependencyProperty TargetsProperty;

	public static readonly DependencyPropertyKey TargetsPropertyKey;
	public static readonly DependencyProperty ViewProperty;
	public static readonly DependencyProperty ZoomLevelProperty;

	private bool _buildRequired;
	private readonly ColumnDefinition _codeCol = new() { Width = OneStar };
	private bool _disposed;
	private IWpfTextViewHost _editor;
	private VsCodeWindowHost _codeWindowHost;
	private bool _firstFrame = true;
	private bool _isPaused;
	private bool _isStarted;
	private bool _loadingTargets;

	/// <summary>
	/// True while this document's tool window / tab is the active (or shown) one.
	/// When false the host is suspended after a short delay to free CPU.
	/// </summary>
	private bool _isDocumentVisible = true;

	/// <summary>
	/// Set before an intentional <see cref="PreviewerProcess.Stop"/> so ProcessExited
	/// does not surface a "preview crashed" banner.
	/// </summary>
	private bool _hostSuspendedIntentionally;

	private DispatcherTimer _backgroundStopTimer;
	private DispatcherTimer _sourceOnlyStopTimer;
	private DispatcherTimer _errorOverlayGraceTimer;
	private DispatcherTimer _forceIncompleteSendTimer;
	private string _pendingForceXaml;

	/// <summary>
	/// Last XAML successfully delivered to the host. Used to skip redundant UpdateXaml.
	/// Cleared when the process exits or is stopped.
	/// </summary>
	private string _lastSentXaml;

	private static Dictionary<string, Task<Metadata>> _metadataCache;
	private static readonly MetadataReader _metadataReader;
	private readonly ColumnDefinition _previewCol = new() { Width = OneStar };
	private Project _project;
	private double _scaling = 1;
	private readonly SemaphoreSlim _startingProcess = new(1, 1);
	private readonly Throttle<string> _throttle;
	private AvaloniaDesignerView _unPausedView;
	private string _xamlPath;

	private static readonly GridLength OneStar;
	private static readonly GridLength ZeroStar;
	private const double ScalingEpsilon = 0.01;

	// Debounce idle delays: longer for large buffers so host reload is less thrashy while typing.
	// Slightly longer default reduces mid-tag pushes (e.g. after typing '<').
	private static readonly TimeSpan XamlDebounceDefault = TimeSpan.FromMilliseconds(500);
	private static readonly TimeSpan XamlDebounceMedium = TimeSpan.FromMilliseconds(650);
	private static readonly TimeSpan XamlDebounceLarge = TimeSpan.FromMilliseconds(800);
	private const int XamlDebounceMediumChars = 40_000;
	private const int XamlDebounceLargeChars = 100_000;

	// Host suspend policy: short delay when tabbing away; longer idle in Source-only mode.
	private static readonly TimeSpan BackgroundStopDelay = TimeSpan.FromSeconds(2);
	private static readonly TimeSpan SourceOnlyStopDelay = TimeSpan.FromSeconds(15);

	// Soften "broke on incomplete XAML": defer error banner; force-send mid-edit after long idle.
	private static readonly TimeSpan ErrorOverlayGrace = TimeSpan.FromMilliseconds(800);
	private static readonly TimeSpan ForceIncompleteSendDelay = TimeSpan.FromMilliseconds(1500);

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="AvaloniaDesigner" /> class.
	/// </summary>
	public AvaloniaDesigner()
	{
		InitializeComponent();
		InitializeToolbarHeader();
		InitializeToolbarIcons();

		// Former XAML: theming:ImageThemingUtilities.ImageBackgroundColor — markup compile
		// cannot resolve that attached property against VS SDK package references.
		// Keep monochrome catalog icons readable on the tool-window background.
		try
		{
			if (Background is SolidColorBrush brush)
			{
				ImageThemingUtilities.SetImageBackgroundColor(this, brush.Color);
			}
		}
		catch (Exception ex)
		{
			Log.Logger.Debug(ex, "ImageThemingUtilities.SetImageBackgroundColor failed");
		}

		_throttle = new Throttle<string>(XamlDebounceDefault, UpdateXaml);

		Process = new PreviewerProcess();
		Process.ErrorChanged += ErrorChanged;
		Process.FrameReceived += FrameReceived;
		Process.ProcessExited += ProcessExited;
		Previewer.Process = Process;
		PausedMessage.Visibility = Visibility.Collapsed;

		UpdateLayoutForView();

		Loaded += OnLoaded;
	}

	static AvaloniaDesigner()
	{
		TargetsPropertyKey = DependencyProperty.RegisterReadOnly(
			nameof(Targets),
			typeof(IReadOnlyList<DesignerRunTarget>),
			typeof(AvaloniaDesigner),
			new PropertyMetadata());
		SelectedTargetProperty = DependencyProperty.Register(
			nameof(SelectedTarget),
			typeof(DesignerRunTarget),
			typeof(AvaloniaDesigner),
			new PropertyMetadata(HandleSelectedTargetChanged));
		SplitOrientationProperty = DependencyProperty.Register(
			nameof(SplitOrientation),
			typeof(Orientation),
			typeof(AvaloniaDesigner),
			new PropertyMetadata(Orientation.Horizontal, HandleSplitOrientationChanged));
		ViewProperty = DependencyProperty.Register(
			nameof(View),
			typeof(AvaloniaDesignerView),
			typeof(AvaloniaDesigner),
			new PropertyMetadata(AvaloniaDesignerView.Split, HandleViewChanged));
		TargetsProperty = TargetsPropertyKey.DependencyProperty;
		ZoomLevelProperty = DependencyProperty.Register(
			nameof(ZoomLevel),
			typeof(string),
			typeof(AvaloniaDesigner),
			new PropertyMetadata("100%", HandleZoomLevelChanged));
		_metadataReader = new(new DnlibMetadataProvider());
		OneStar = new(1, GridUnitType.Star);
		ZeroStar = new(0, GridUnitType.Star);
		ZoomLevels = VisualStudio.ZoomLevels.Levels;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the paused state of the designer.
	/// </summary>
	public bool IsPaused
	{
		get => _isPaused;
		set
		{
			if (_isPaused != value)
			{
				Log.Logger.Debug("Setting pause state to {State}", value);

				_isPaused = value;
				StartStopProcessAsync().FireAndForget();

				if (value)
				{
					_unPausedView = View;

					// Hide the designer and only show the xaml source when debugging
					// This matches UWP/WPF's designer
					View = AvaloniaDesignerView.Source;
				}
				else
				{
					View = _unPausedView;
				}
			}
		}
	}

	/// <summary>
	/// Gets or sets whether the split view panes are swapped.
	/// </summary>
	public bool PreviewAndXamlPanesSwapped
	{
		get =>
			SplitOrientation switch
			{
				Orientation.Horizontal => Grid.GetRow(EditorHost) != 0,
				Orientation.Vertical => Grid.GetColumn(EditorHost) != 0,
				_ => throw new NotSupportedException()
			};

		set
		{
			if (value == PreviewAndXamlPanesSwapped)
			{
				return;
			}

			switch (SplitOrientation)
			{
				case Orientation.Horizontal:
					if (value)
					{
						Grid.SetRow(EditorHost, 2);
						Grid.SetRow(Previewer, 0);
					}
					else
					{
						Grid.SetRow(EditorHost, 0);
						Grid.SetRow(Previewer, 2);
					}
					break;

				case Orientation.Vertical:
					if (value)
					{
						Grid.SetColumn(EditorHost, 2);
						Grid.SetColumn(Previewer, 0);
					}
					else
					{
						Grid.SetColumn(EditorHost, 0);
						Grid.SetColumn(Previewer, 2);
					}
					break;

				default:
					throw new NotSupportedException();
			}
		}
	}

	/// <summary>
	/// Gets the previewer process used by the designer.
	/// </summary>
	public PreviewerProcess Process { get; }

	/// <summary>
	/// True when the Avalonia design host process is currently running.
	/// Used by the editor pane to mark the document tab (e.g. <c>* File.axaml</c>).
	/// </summary>
	public bool IsPreviewHostRunning => !_disposed && Process.IsRunning;

	/// <summary>
	/// Raised when <see cref="IsPreviewHostRunning"/> changes.
	/// </summary>
	public event EventHandler PreviewHostRunningChanged;

	private bool? _notifiedHostRunning;

	/// <summary>
	/// Gets or sets the selected target.
	/// </summary>
	public DesignerRunTarget SelectedTarget
	{
		get => (DesignerRunTarget) GetValue(SelectedTargetProperty);
		set => SetValue(SelectedTargetProperty, value);
	}

	/// <summary>
	/// Gets or sets the orientation of the split view.
	/// </summary>
	public Orientation SplitOrientation
	{
		get => (Orientation) GetValue(SplitOrientationProperty);
		set => SetValue(SplitOrientationProperty, value);
	}

	/// <summary>
	/// Gets the list of targets that the designer can use to preview the XAML.
	/// </summary>
	public IReadOnlyList<DesignerRunTarget> Targets
	{
		get => (IReadOnlyList<DesignerRunTarget>) GetValue(TargetsProperty);
		private set => SetValue(TargetsPropertyKey, value);
	}

	/// <summary>
	/// Gets or sets the type of view to display.
	/// </summary>
	public AvaloniaDesignerView View
	{
		get => (AvaloniaDesignerView) GetValue(ViewProperty);
		set => SetValue(ViewProperty, value);
	}

	/// <summary>
	/// Gets or sets the zoom level as a string.
	/// </summary>
	public string ZoomLevel
	{
		get => (string) GetValue(ZoomLevelProperty);
		set => SetValue(ZoomLevelProperty, value);
	}

	public static string[] ZoomLevels { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Disposes of the designer and all resources.
	/// </summary>
	public void Dispose()
	{
		_disposed = true;

		StopAndDisposeTimer(ref _backgroundStopTimer);
		StopAndDisposeTimer(ref _sourceOnlyStopTimer);
		StopAndDisposeTimer(ref _errorOverlayGraceTimer);
		StopAndDisposeTimer(ref _forceIncompleteSendTimer);

		if (_editor?.TextView.TextBuffer is ITextBuffer2 oldBuffer)
		{
			oldBuffer.ChangedOnBackground -= TextChanged;
		}

		if (_editor?.IsClosed == false)
		{
			_editor.Close();
		}

		Process.FrameReceived -= FrameReceived;

		_codeWindowHost?.Dispose();
		_codeWindowHost = null;

		_throttle.Dispose();
		Previewer.Dispose();
		Process.Dispose();
		NotifyPreviewHostRunningChanged();
	}

	/// <summary>
	/// Sets the toolbar title to the package name and current extension version.
	/// </summary>
	private void InitializeToolbarHeader()
	{
		var version = CornerstoneConstants.PackageVersion;
		HeaderTitle.Text = string.IsNullOrEmpty(version)
			? CornerstoneConstants.PackageName
			: $"{CornerstoneConstants.PackageName} {version}";
	}

	/// <summary>
	/// Assigns VS catalog moniker icons to the designer toolbar controls.
	/// </summary>
	/// <remarks>
	/// Built in code rather than XAML because MarkupCompilePass1 cannot resolve
	/// Microsoft.VisualStudio.Imaging types from the VS SDK package references.
	/// Monikers match the former XAML: Splitter, HTMLDesignView, MarkupTag,
	/// SplitScreenHorizontally, SplitScreenVertically, SwitchSourceOrTarget.
	/// </remarks>
	private void InitializeToolbarIcons()
	{
		ViewSplitItem.Content = CreateToolbarIcon(KnownMonikers.Splitter);
		ViewDesignItem.Content = CreateToolbarIcon(KnownMonikers.HTMLDesignView);
		ViewSourceItem.Content = CreateToolbarIcon(KnownMonikers.MarkupTag);
		OrientationHorizontalItem.Content = CreateToolbarIcon(KnownMonikers.SplitScreenHorizontally);
		OrientationVerticalItem.Content = CreateToolbarIcon(KnownMonikers.SplitScreenVertically);
		SwapPanesButton.Content = CreateToolbarIcon(KnownMonikers.SwitchSourceOrTarget);
	}

	/// <summary>
	/// Creates a themed toolbar glyph for the given catalog moniker.
	/// </summary>
	private static CrispImage CreateToolbarIcon(ImageMoniker moniker)
	{
		return new CrispImage
		{
			Moniker = moniker,
			Width = 16,
			Height = 16,
			RenderTransformOrigin = new Point(0.5, 0.5)
		};
	}

	/// <summary>
	/// Notifies listeners when the host process running state changes (tab caption, etc.).
	/// </summary>
	private void NotifyPreviewHostRunningChanged()
	{
		var running = IsPreviewHostRunning;
		if (_notifiedHostRunning == running)
		{
			return;
		}

		_notifiedHostRunning = running;
		try
		{
			PreviewHostRunningChanged?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			Log.Logger.Debug(ex, "PreviewHostRunningChanged handler failed");
		}
	}

	/// <summary>
	/// Called by the editor pane when the document tab / frame is shown or hidden.
	/// Suspends the 60 Hz design host while the tab is not visible.
	/// </summary>
	public void SetDocumentVisible(bool visible)
	{
		if (_disposed || (_isDocumentVisible == visible))
		{
			return;
		}

		_isDocumentVisible = visible;
		Log.Logger.Debug("AvaloniaDesigner document visible = {Visible}", visible);

		if (visible)
		{
			StopTimer(_backgroundStopTimer);
			// Resume host for the current view mode (if not build/debug paused).
			StartStopProcessAsync().FireAndForget();
		}
		else
		{
			// Short delay avoids thrashing if VS fires transient hide/show pairs.
			EnsureBackgroundStopTimer().Stop();
			EnsureBackgroundStopTimer().Start();
		}
	}

	/// <summary>
	/// Called when the solution/project build finishes. Clears design-time pause, reloads
	/// host targets, and restarts the previewer process so rebuilt assemblies (C# design
	/// data, code-behind) are loaded. <see cref="PreviewerProcess.UpdateXamlAsync"/> alone
	/// only re-parses XAML against already-loaded types.
	/// </summary>
	public async Task OnBuildCompletedAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		// Clear pause without going through the IsPaused setter (that races Stop vs Start).
		// When paused, the setter stashes Split/Design/Source in _unPausedView and forces Source.
		if (_isPaused)
		{
			_isPaused = false;
			View = _unPausedView;
		}

		PausedMessage.Visibility = Visibility.Collapsed;

		if (_disposed || !_isStarted || !IsLoaded)
		{
			return;
		}

		// Backgrounded tabs stay suspended; resume when the document becomes visible again.
		if (!_isDocumentVisible)
		{
			Log.Logger.Debug("Build completed; document not visible — host stays suspended");
			return;
		}

		await RecycleHostAsync("build completed");
	}

	/// <summary>
	/// Invalidates the intellisense completion metadata.
	/// </summary>
	/// <remarks>
	/// Should be called when the designer is paused; when unpaused the completion metadata
	/// will be updated.
	/// </remarks>
	public void InvalidateCompletionMetadata()
	{
		var buffer = _editor.TextView.TextBuffer;

		if (buffer.Properties.TryGetProperty<XamlBufferMetadata>(
				typeof(XamlBufferMetadata),
				out var metadata))
		{
			metadata.NeedInvalidation = true;
		}
	}

	/// <summary>
	/// Starts the designer.
	/// </summary>
	/// <param name="project"> The project containing the XAML file. </param>
	/// <param name="xamlPath"> The path to the XAML file. </param>
	/// <param name="editor"> The VS text editor control host. </param>
	/// <param name="codeWindow">
	/// Optional full <see cref="IVsCodeWindow"/> so the native code splitter can be hosted.
	/// </param>
	/// <param name="oleServiceProvider"> Site used when hosting the full code window. </param>
	public void Start(
		Project project,
		string xamlPath,
		IWpfTextViewHost editor,
		IVsCodeWindow codeWindow = null,
		Microsoft.VisualStudio.OLE.Interop.IServiceProvider oleServiceProvider = null)
	{
		Log.Logger.Verbose("Started AvaloniaDesigner.Start()");

		if (_isStarted)
		{
			throw new InvalidOperationException("The designer has already been started.");
		}

		_project = project ?? throw new ArgumentNullException(nameof(project));
		_xamlPath = xamlPath ?? throw new ArgumentNullException(nameof(xamlPath));
		_editor = editor ?? throw new ArgumentNullException(nameof(editor));

		InitializeEditor(codeWindow, oleServiceProvider);
		LoadTargetsAndStartProcessAsync().FireAndForget();

		Log.Logger.Verbose("Finished AvaloniaDesigner.Start()");
	}

	/// <summary>
	/// Parses the current <see cref="ZoomLevel"/> as a fixed percentage (e.g. <c>100%</c>).
	/// Fit modes were removed to avoid viewport ↔ host scaling feedback loops.
	/// </summary>
	public bool TryProcessZoomLevelValue(out double scaling)
	{
		scaling = 1;
		var zoomLevel = ZoomLevel;
		if (string.IsNullOrEmpty(zoomLevel))
		{
			return false;
		}

		// Legacy Fit settings (pre-removal) map to 100% and rewrite the combo text.
		if (zoomLevel.StartsWith("Fit", StringComparison.OrdinalIgnoreCase))
		{
			if (!string.Equals(ZoomLevel, "100%", StringComparison.Ordinal))
			{
				ZoomLevel = "100%";
			}

			scaling = 1;
			return true;
		}

		if (double.TryParse(zoomLevel.TrimEnd('%'), NumberStyles.Number, CultureInfo.InvariantCulture, out var zoomPercent)
			&& (zoomPercent > 0) && (zoomPercent <= 1000))
		{
			scaling = zoomPercent / 100;
			return true;
		}

		return false;
	}

	protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
	{
		Process.SetScalingAsync(newDpi.DpiScaleX * _scaling).FireAndForget();
	}

	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		if (e.Property == SelectedTargetProperty)
		{
			Previewer.SelectedProject = SelectedTarget.Project;
		}
		base.OnPropertyChanged(e);
	}

	private static async Task CreateCompletionMetadataAsync(
		string executablePath,
		Func<IAssemblyProvider> assemblyProviderFunc,
		XamlBufferMetadata target)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		if (_metadataCache == null)
		{
			_metadataCache = new Dictionary<string, Task<Metadata>>();
			var dte = (DTE) Package.GetGlobalService(typeof(DTE));

			dte.Events.BuildEvents.OnBuildBegin += (s, e) => _metadataCache.Clear();
		}

		Log.Logger.Information("Started AvaloniaDesigner.CreateCompletionMetadataAsync() for {ExecutablePath}", executablePath);

		try
		{
			var sw = Stopwatch.StartNew();

			Task<Metadata> metadataLoad;

			if (!_metadataCache.TryGetValue(executablePath, out metadataLoad))
			{
				var assemblyProvider = assemblyProviderFunc();
				metadataLoad = Task.Run(() => _metadataReader.GetForTargetAssembly(assemblyProvider));
				_metadataCache[executablePath] = metadataLoad;
			}

			target.CompletionMetadata = await metadataLoad;

			target.NeedInvalidation = false;

			sw.Stop();

			Log.Logger.Verbose("Finished AvaloniaDesigner.CreateCompletionMetadataAsync() took {Time} for {ExecutablePath}", sw.Elapsed, executablePath);
		}
		catch (Exception ex)
		{
			Log.Logger.Error(ex, "Error creating XAML completion metadata");
		}
		finally
		{
			Log.Logger.Verbose("Finished AvaloniaDesigner.CreateCompletionMetadataAsync()");
		}
	}

	private async void ErrorChanged(object sender, EventArgs e)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		if (_disposed)
		{
			return;
		}

		if (Process.Error != null)
		{
			// Host freezes the last good frame immediately (no pixel thrash).
			// Defer the "Invalid Markup" banner so brief mid-edit failures don't flash.
			var timer = EnsureErrorOverlayGraceTimer();
			timer.Stop();
			timer.Start();
			return;
		}

		StopTimer(_errorOverlayGraceTimer);
		ShowPreview();
	}

	private DispatcherTimer EnsureErrorOverlayGraceTimer()
	{
		if (_errorOverlayGraceTimer == null)
		{
			_errorOverlayGraceTimer = new DispatcherTimer
			{
				Interval = ErrorOverlayGrace
			};
			_errorOverlayGraceTimer.Tick += (_, __) =>
			{
				_errorOverlayGraceTimer.Stop();
				if (_disposed || (Process.Error == null))
				{
					return;
				}

				// Still invalid after grace — surface the banner.
				ShowError("Invalid Markup", FormatMarkupError(Process.Error));
			};
		}

		return _errorOverlayGraceTimer;
	}

	private DispatcherTimer EnsureForceIncompleteSendTimer()
	{
		if (_forceIncompleteSendTimer == null)
		{
			_forceIncompleteSendTimer = new DispatcherTimer
			{
				Interval = ForceIncompleteSendDelay
			};
			_forceIncompleteSendTimer.Tick += (_, __) =>
			{
				_forceIncompleteSendTimer.Stop();
				if (_disposed || string.IsNullOrEmpty(_pendingForceXaml))
				{
					return;
				}

				var xaml = _pendingForceXaml;
				_pendingForceXaml = null;
				// Long idle on incomplete markup — push anyway so real errors can surface.
				Log.Logger.Verbose("Force-sending incomplete XAML after idle ({Length} chars)", xaml.Length);
				PushXamlToHost(xaml, forceIncomplete: true);
			};
		}

		return _forceIncompleteSendTimer;
	}

	private void FrameReceived(object sender, EventArgs e)
	{
		// Raised on the UI thread by PreviewerProcess after pixels are written.
		// Frames are suppressed while markup is invalid (preview is frozen).
		if ((Process.Bitmap == null) || Process.IsMarkupPaused)
		{
			return;
		}

		if (_firstFrame)
		{
			_firstFrame = false;
			if (TryProcessZoomLevelValue(out var scaling))
			{
				UpdateScaling(scaling);
			}
		}

		ShowPreview();
	}

	private static string FormatMarkupError(Avalonia.Remote.Protocol.Designer.ExceptionDetails error)
	{
		if (error == null)
		{
			return "Check the Error List for more information.";
		}

		var message = error.Message;
		if (string.IsNullOrWhiteSpace(message))
		{
			message = "Invalid XAML.";
		}

		// Host sometimes returns full exception ToString(); keep the first line for the banner.
		var firstLine = message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
			?? message;

		if (error.LineNumber is int line && line > 0)
		{
			var col = error.LinePosition is int c && c > 0 ? $", col {c}" : string.Empty;
			return $"Line {line}{col}: {firstLine}\n\nPreview is paused on the last valid frame. Fix the markup to resume.";
		}

		return $"{firstLine}\n\nPreview is paused on the last valid frame. Fix the markup to resume.";
	}

	private string GetMSBuildProperty(string key, IVsBuildPropertyStorage storage)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		var hr = storage.GetPropertyValue(key, null, (uint) _PersistStorageType.PST_USER_FILE, out var value);
		var E_XML_ATTRIBUTE_NOT_FOUND = unchecked((int) 0x8004C738);

		// ignore this HR, it means that there's no value for this key
		if (hr != E_XML_ATTRIBUTE_NOT_FOUND)
		{
			Marshal.ThrowExceptionForHR(hr);
		}

		return value;
	}

	private IVsBuildPropertyStorage GetMSBuildPropertyStorage(Project project)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		var solution = (IVsSolution) ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution));

		var hr = solution.GetProjectOfUniqueName(project.FullName, out var hierarchy);
		Marshal.ThrowExceptionForHR(hr);

		return hierarchy as IVsBuildPropertyStorage;
	}

	private string GetReferencesFilePath(IVsBuildPropertyStorage storage)
	{
		// .NET 8 SDK Artifacts output layout
		// https://learn.microsoft.com/en-us/dotnet/core/sdk/artifacts-output
		// Example
		// MSBuildProjectDirectory: X:\abcd\src\Mobius.Windows\
		// IntermediateOutputPath: X:\abcd\src\artifacts\obj\Mobius.Windows\debug_net8.0-windows10.0.26100.0

		var intermediateOutputPath = GetMSBuildProperty("IntermediateOutputPath", storage);
		if (Path.IsPathRooted(intermediateOutputPath))
		{
			return Path.Combine(intermediateOutputPath, "Avalonia", "references");
		}
		var projDir = GetMSBuildProperty("MSBuildProjectDirectory", storage);
		return Path.Combine(projDir, intermediateOutputPath.TrimStart(Path.DirectorySeparatorChar), "Avalonia", "references");
	}

	private static void HandleSelectedTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AvaloniaDesigner designer && !designer._loadingTargets)
		{
			designer.SelectedTargetChangedAsync(d, e).FireAndForget();
		}
	}

	private static void HandleSplitOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AvaloniaDesigner designer)
		{
			designer.UpdateLayoutForView();
		}
	}

	private static void HandleViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AvaloniaDesigner designer)
		{
			designer.UpdateLayoutForView();
			designer.OnViewModeChanged();
		}
	}

	/// <summary>
	/// Source-only mode keeps the host alive only briefly (for error tagger / first paint),
	/// then suspends after idle. Design/Split keep the host while the tab is visible.
	/// </summary>
	private void OnViewModeChanged()
	{
		if (_disposed || !_isStarted || IsPaused || !_isDocumentVisible)
		{
			return;
		}

		if (View == AvaloniaDesignerView.Source)
		{
			ArmSourceOnlySuspendTimer();
		}
		else
		{
			StopTimer(_sourceOnlyStopTimer);
			if (!Process.IsRunning && IsLoaded)
			{
				StartStopProcessAsync().FireAndForget();
			}
		}
	}

	private static void HandleZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AvaloniaDesigner designer && designer.TryProcessZoomLevelValue(out var scaling))
		{
			designer.UpdateScaling(scaling);
		}
	}

	private void InitializeEditor(
		IVsCodeWindow codeWindow,
		Microsoft.VisualStudio.OLE.Interop.IServiceProvider oleServiceProvider)
	{
		// Prefer hosting the full IVsCodeWindow so Window → Split / the scrollbar split grip
		// work. Reparenting only WpfTextViewHost.HostControl strips that chrome (and is why
		// AvaloniaVS set CWB_DISABLESPLITTER historically).
		if (codeWindow != null && oleServiceProvider != null)
		{
			try
			{
				_codeWindowHost = new VsCodeWindowHost(codeWindow, oleServiceProvider);
				_codeWindowHost.HostFailed += (_, ex) =>
				{
					Log.Logger.Warning(ex, "Full IVsCodeWindow host failed; falling back to text view host");
					_codeWindowHost?.Dispose();
					_codeWindowHost = null;
					HostTextViewOnly();
				};
				EditorHost.Child = _codeWindowHost;
				Log.Logger.Debug("Hosting full IVsCodeWindow (native code splitter enabled)");
			}
			catch (Exception ex)
			{
				Log.Logger.Warning(ex, "Full IVsCodeWindow host failed; falling back to text view host");
				_codeWindowHost?.Dispose();
				_codeWindowHost = null;
				HostTextViewOnly();
			}
		}
		else
		{
			HostTextViewOnly();
		}

		_editor.TextView.TextBuffer.Properties.RemoveProperty(typeof(PreviewerProcess));
		_editor.TextView.TextBuffer.Properties.AddProperty(typeof(PreviewerProcess), Process);

		_editor.TextView.Properties.RemoveProperty(typeof(AvaloniaDesigner));
		_editor.TextView.Properties.AddProperty(typeof(AvaloniaDesigner), this);

		if (_editor.TextView.TextBuffer is ITextBuffer2 newBuffer)
		{
			newBuffer.ChangedOnBackground += TextChanged;
		}
	}

	/// <summary>
	/// Legacy path: embed only the WPF text view (no native Window → Split chrome).
	/// </summary>
	private void HostTextViewOnly()
	{
		var hostControl = _editor.HostControl as FrameworkElement;
		var parent = VisualTreeHelper.GetParent(hostControl) as FrameworkElement;

		FrameworkElement elementToReparent = hostControl;

		if (parent != null)
		{
			elementToReparent = parent;
			var grandParent = VisualTreeHelper.GetParent(parent);

			if (grandParent is Panel panel)
			{
				panel.Children.Remove(parent);
			}
			else if (grandParent is Decorator decorator)
			{
				decorator.Child = null;
			}
		}

		EditorHost.Child = elementToReparent;
	}

	private async Task LoadTargetsAndStartProcessAsync()
	{
		Log.Logger.Verbose("Started AvaloniaDesigner.LoadTargetsAndStartProcessAsync()");

		await LoadTargetsAsync();

		if (!_disposed)
		{
			_isStarted = true;
			await StartStopProcessAsync();
		}

		Log.Logger.Verbose("Finished AvaloniaDesigner.LoadTargetsAndStartProcessAsync()");
	}

	private async Task LoadTargetsAsync()
	{
		Log.Logger.Verbose("Started AvaloniaDesigner.LoadTargetsAsync()");

		_loadingTargets = true;

		try
		{
			var projects = await CornerstonePackage.SolutionService.GetProjectsAsync();
			var xamlProjectInfo = projects.FirstOrDefault(x => x.Project == _project);
			var xamlProjectName = xamlProjectInfo?.Name ?? _project?.Name ?? string.Empty;

			// Host candidates: Avalonia desktop executables that reference this XAML project
			// (or are the XAML project itself). Web / mobile / headless hosts are excluded.
			bool IsValidTarget(ProjectInfo project)
			{
				if (!project.IsAvaloniaDesktopHostCandidate)
				{
					return false;
				}

				// Self: the AXAML lives in an Avalonia desktop app.
				if (project.Project == _project)
				{
					return true;
				}

				// Host must reference the library that owns the AXAML (direct or transitive).
				return project.ProjectReferences.Contains(_project);
			}

			bool IsValidOutput(ProjectOutputInfo output)
			{
				if (string.IsNullOrWhiteSpace(output.HostApp))
				{
					return false;
				}

				if (!output.IsNetCore && !output.IsNetFramework)
				{
					return false;
				}

				// Browser / mobile cannot run the desktop designer host.
				if (string.Equals(output.RuntimeIdentifier, "browser-wasm", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(output.TargetPlatformIdentifier, "browser", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(output.TargetPlatformIdentifier, "android", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(output.TargetPlatformIdentifier, "ios", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(output.TargetPlatformIdentifier, "tvos", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}

				// Prefer classic desktop platforms (empty TPI is fine for netX.Y desktop TFMs).
				return (output.TargetPlatformIdentifier == "")
					|| string.Equals(output.TargetPlatformIdentifier, "windows", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(output.TargetPlatformIdentifier, "macos", StringComparison.OrdinalIgnoreCase);
			}

			string GetXamlAssembly()
			{
				// Prefer the library's own netcore output, then netstandard.
				return xamlProjectInfo?.Outputs
					.OrderBy(x => !x.IsNetCore)
					.ThenBy(x => !x.IsNetStandard)
					.FirstOrDefault()?
					.TargetAssembly;
			}

			// Ranking (lower is better):
			//  0) The XAML project itself (it is an Avalonia desktop app)
			//  1) Sibling / consumer named *.Desktop (Avalonia multi-target convention)
			//  2) Sibling / consumer named *.Windows / *.Win / *.Mac / *.Linux
			//  3) Name starts with the library name (e.g. CannonFarm.Desktop for CannonFarm)
			//  4) Has Avalonia.Desktop / Win32 / Native stack (strong desktop signal)
			//  5) Solution startup project
			//  6) Direct project reference (not only transitive)
			//  7) Everything else, alphabetically
			int RankHost(ProjectInfo project)
			{
				if (project.Project == _project)
				{
					return 0;
				}

				var name = project.Name ?? string.Empty;
				if (IsDesktopHostName(name, xamlProjectName))
				{
					return 1;
				}

				if (IsPlatformHostName(name, xamlProjectName))
				{
					return 2;
				}

				if (!string.IsNullOrEmpty(xamlProjectName) &&
					name.StartsWith(xamlProjectName + ".", StringComparison.OrdinalIgnoreCase))
				{
					return 3;
				}

				if (project.HasAvaloniaDesktop)
				{
					return 4;
				}

				if (project.IsStartupProject)
				{
					return 5;
				}

				// Prefer hosts that reference the library directly over deep transitive ones.
				if (project.DirectProjectReferences?.Contains(_project) == true)
				{
					return 6;
				}

				return 7;
			}

			int RankOutput(ProjectOutputInfo output)
			{
				// Prefer netX.Y-windows / desktop TFMs over plain netX.Y when both exist.
				var score = 0;
				if (string.Equals(output.TargetPlatformIdentifier, "windows", StringComparison.OrdinalIgnoreCase))
				{
					score -= 2;
				}
				else if (string.Equals(output.TargetPlatformIdentifier, "macos", StringComparison.OrdinalIgnoreCase))
				{
					score -= 1;
				}

				if (output.IsNetCore)
				{
					score -= 1;
				}

				return score;
			}

			var oldSelectedTarget = SelectedTarget;
			var xamlAssembly = GetXamlAssembly();

			var ranked = (from project in projects
				where IsValidTarget(project)
				from output in project.Outputs
				where IsValidOutput(output)
				let hostRank = RankHost(project)
				let outputRank = RankOutput(output)
				orderby hostRank, outputRank, project.Name, output.TargetFramework
				select new DesignerRunTarget
				{
					Name = $"{project.Name} [{output.TargetFramework}]",
					ExecutableAssembly = output.TargetAssembly,
					XamlAssembly = xamlAssembly ?? output.TargetAssembly,
					HostApp = output.HostApp,
					Project = project.Project,
					IsNetFramework = output.IsNetFramework
				}).ToList();

			Targets = ranked;

			SelectedTarget = Targets.FirstOrDefault(t => t.Name == oldSelectedTarget?.Name)
				?? Targets.FirstOrDefault();

			if (Targets.Count == 0)
			{
				Log.Logger.Warning(
					"No Avalonia desktop host found for project {Project}. " +
					"Need an executable Avalonia.Desktop app that references this project " +
					"(e.g. YourApp.Desktop), or open AXAML inside a desktop app project.",
					xamlProjectName);
			}
			else
			{
				Log.Logger.Debug(
					"Designer hosts for {Project}: selected {Selected}; candidates: {Candidates}",
					xamlProjectName,
					SelectedTarget?.Name,
					string.Join(", ", Targets.Select(t => t.Name)));
			}
		}
		finally
		{
			_loadingTargets = false;
		}

		Log.Logger.Verbose("Finished AvaloniaDesigner.LoadTargetsAsync()");
	}

	/// <summary>
	/// Avalonia multi-platform convention: shared project + <c>Something.Desktop</c> host.
	/// </summary>
	private static bool IsDesktopHostName(string hostName, string xamlProjectName)
	{
		if (string.IsNullOrEmpty(hostName))
		{
			return false;
		}

		if (hostName.EndsWith(".Desktop", StringComparison.OrdinalIgnoreCase) ||
			hostName.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// e.g. CannonFarm.Desktop when editing CannonFarm or CannonFarm.Controls
		if (!string.IsNullOrEmpty(xamlProjectName) &&
			hostName.Equals(xamlProjectName + ".Desktop", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// Shared library named Foo.Something → Foo.Desktop
		var root = GetProjectRootName(xamlProjectName);
		return !string.IsNullOrEmpty(root) &&
			hostName.Equals(root + ".Desktop", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsPlatformHostName(string hostName, string xamlProjectName)
	{
		if (string.IsNullOrEmpty(hostName))
		{
			return false;
		}

		string[] suffixes = [".Windows", ".Win", ".Mac", ".MacOS", ".Linux"];
		foreach (var suffix in suffixes)
		{
			if (hostName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		var root = GetProjectRootName(xamlProjectName);
		if (string.IsNullOrEmpty(root))
		{
			return false;
		}

		foreach (var suffix in suffixes)
		{
			if (hostName.Equals(root + suffix, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// <c>CannonFarm.Controls</c> → <c>CannonFarm</c>; <c>CannonFarm</c> → <c>CannonFarm</c>.
	/// </summary>
	private static string GetProjectRootName(string projectName)
	{
		if (string.IsNullOrEmpty(projectName))
		{
			return projectName;
		}

		var dot = projectName.IndexOf('.');
		return dot > 0 ? projectName.Substring(0, dot) : projectName;
	}

	private void OnLoaded(object s, RoutedEventArgs e)
	{
		StartStopProcessAsync().FireAndForget();
	}

	private async void ProcessExited(object sender, EventArgs e)
	{
		// Intentional suspend (tab background / Source-only idle / pause / target change).
		var intentional = _hostSuspendedIntentionally;
		_hostSuspendedIntentionally = false;

		if (intentional || _disposed)
		{
			// Next successful start must re-send XAML even if text is unchanged.
			if (!Process.IsRunning)
			{
				_firstFrame = true;
				_lastSentXaml = null;
			}

			NotifyPreviewHostRunningChanged();
			return;
		}

		// Stale Exited from a process we already replaced — ignore if a host is running again.
		if (Process.IsRunning || IsPaused)
		{
			NotifyPreviewHostRunningChanged();
			return;
		}

		_firstFrame = true;
		_lastSentXaml = null;
		NotifyPreviewHostRunningChanged();

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		// Keep any frozen frame visible; resume by editing (restarts host) or rebuilding.
		ShowError(
			"Preview Paused",
			"The previewer process exited. The last valid frame is kept on screen.\n\n" +
			"Edit the XAML to retry, or rebuild the project if assemblies are missing. " +
			"See the Cornerstone Diagnostics output pane for details.");
	}

	private static async Task<string> ReadAllTextAsync(string fileName)
	{
		using (var reader = File.OpenText(fileName))
		{
			return await reader.ReadToEndAsync();
		}
	}

	private void RebuildMetadata(string assemblyPath, string executablePath)
	{
		assemblyPath ??= SelectedTarget?.XamlAssembly;
		var project = SelectedTarget?.Project;

		if ((assemblyPath != null) && (project != null))
		{
			var buffer = _editor.TextView.TextBuffer;
			var metadata = buffer.Properties.GetOrCreateSingletonProperty(
				typeof(XamlBufferMetadata),
				() => new XamlBufferMetadata());
			buffer.Properties["AssemblyName"] = Path.GetFileNameWithoutExtension(assemblyPath);

			if ((metadata.CompletionMetadata == null) || metadata.NeedInvalidation)
			{
				Func<IAssemblyProvider> assemblyProviderFunc = () =>
				{
					if (VsProjectAssembliesProvider.TryCreate(project, assemblyPath) is { } vsProjectAsmProvider)
					{
						return vsProjectAsmProvider;
					}
					if (GetReferencesFilePath(GetMSBuildPropertyStorage(project)) is { } referencesPath
						&& File.Exists(referencesPath))
					{
						return new ReferenceFileAssemblyProvider(referencesPath, assemblyPath);
					}
					return new DepsJsonFileAssemblyProvider(executablePath, assemblyPath);
				};

				CreateCompletionMetadataAsync(executablePath, assemblyProviderFunc, metadata).FireAndForget();
			}
		}
	}

	private async Task SelectedTargetChangedAsync(object sender, DependencyPropertyChangedEventArgs e)
	{
		var oldValue = (DesignerRunTarget) e.OldValue;
		var newValue = (DesignerRunTarget) e.NewValue;

		Log.Logger.Debug(
			"AvaloniaDesigner.SelectedTarget changed from {OldTarget} to {NewTarget}",
			oldValue?.ExecutableAssembly,
			newValue?.ExecutableAssembly);

		if (oldValue?.ExecutableAssembly != newValue?.ExecutableAssembly)
		{
			if (_isStarted)
			{
				try
				{
					Log.Logger.Debug("Waiting for StartProcessAsync to finish");
					await _startingProcess.WaitAsync();
					// Intentional recycle for new target — not a crash.
					SuspendHost("selected target changed");
					if (_isDocumentVisible && !IsPaused)
					{
						StartProcessAsync().FireAndForget();
					}
				}
				finally
				{
					_startingProcess.Release();
				}
			}
		}
	}

	private void ShowError(string heading, string message)
	{
		ErrorIndicator.Visibility = Visibility.Visible;
		ErrorHeading.Text = heading;
		ErrorMessage.Text = message;
		if (_buildRequired)
		{
			Previewer.BuildButton.Visibility = Visibility.Visible;
		}
		else
		{
			Previewer.BuildButton.Visibility = Visibility.Hidden;
		}
		Previewer.Error.Visibility = Visibility.Visible;
		Previewer.ErrorHeading.Text = heading;
		Previewer.ErrorMessage.Text = message;
	}

	private void ShowPreview()
	{
		ErrorIndicator.Visibility = Visibility.Collapsed;
		Previewer.Error.Visibility = Visibility.Collapsed;
		Previewer.BuildButton.Visibility = Visibility.Hidden;
	}

	/// <summary>
	/// Fully stops the host (waiting for exit), refreshes targets, and starts a new process.
	/// Used after builds so C# / design-data changes load; also safe after target changes.
	/// </summary>
	private async Task RecycleHostAsync(string reason)
	{
		Log.Logger.Information("Recycling previewer host ({Reason})", reason);

		await _startingProcess.WaitAsync();
		try
		{
			if (_disposed)
			{
				return;
			}

			_hostSuspendedIntentionally = true;
			_lastSentXaml = null;
			_firstFrame = true;

			try
			{
				await Process.StopAndWaitAsync(TimeSpan.FromSeconds(5));
			}
			catch (Exception ex)
			{
				Log.Logger.Debug(ex, "StopAndWaitAsync during host recycle");
			}

			_hostSuspendedIntentionally = false;
			NotifyPreviewHostRunningChanged();

			if (_disposed || IsPaused || !_isDocumentVisible || !IsLoaded)
			{
				return;
			}

			// Refresh assembly paths after build (TFM outputs / HostApp may change).
			await LoadTargetsAsync();

			if (SelectedTarget == null)
			{
				Log.Logger.Error("No Avalonia desktop host found after recycle");
				ShowError(
					"No Avalonia Desktop Host",
					"The designer needs an Avalonia desktop app (OutputType Exe/WinExe with Avalonia.Desktop) " +
					"that references this project — typically YourApp.Desktop.\n\n" +
					"Web, browser, and mobile projects cannot host the previewer.\n" +
					"If the solution is still loading, wait and rebuild, then reopen the file.");
				return;
			}

			await StartProcessCoreAsync();
		}
		finally
		{
			NotifyPreviewHostRunningChanged();
			_startingProcess.Release();
		}
	}

	private async Task StartProcessAsync()
	{
		Log.Logger.Verbose("Started AvaloniaDesigner.StartProcessAsync()");

		ShowPreview();

		if (SelectedTarget == null)
		{
			Log.Logger.Error("No Avalonia desktop host found for preview");

			ShowError(
				"No Avalonia Desktop Host",
				"The designer needs an Avalonia desktop app (OutputType Exe/WinExe with Avalonia.Desktop) " +
				"that references this project — typically YourApp.Desktop.\n\n" +
				"Web, browser, and mobile projects cannot host the previewer.\n" +
				"If the solution is still loading, wait and rebuild, then reopen the file.");
			Log.Logger.Verbose("Finished AvaloniaDesigner.StartProcessAsync()");
			return;
		}

		try
		{
			await _startingProcess.WaitAsync();

			if (!IsPaused && !_disposed)
			{
				await StartProcessCoreAsync();
			}
		}
		finally
		{
			NotifyPreviewHostRunningChanged();
			_startingProcess.Release();
		}

		Log.Logger.Verbose("Finished AvaloniaDesigner.StartProcessAsync()");
	}

	/// <summary>
	/// Starts the host and pushes current XAML. Caller must hold <see cref="_startingProcess"/>.
	/// </summary>
	private async Task StartProcessCoreAsync()
	{
		var assemblyPath = SelectedTarget?.XamlAssembly;
		var executablePath = SelectedTarget?.ExecutableAssembly;
		var hostAppPath = SelectedTarget?.HostApp;
		var isNetFx = SelectedTarget?.IsNetFramework;

		if ((assemblyPath == null) || (executablePath == null) || (hostAppPath == null) || (isNetFx == null))
		{
			Log.Logger.Error("No Avalonia desktop host found for preview");

			ShowError(
				"No Avalonia Desktop Host",
				"The designer needs an Avalonia desktop app (OutputType Exe/WinExe with Avalonia.Desktop) " +
				"that references this project — typically YourApp.Desktop.\n\n" +
				"Web, browser, and mobile projects cannot host the previewer.\n" +
				"If the solution is still loading, wait and rebuild, then reopen the file.");
			return;
		}

		RebuildMetadata(assemblyPath, executablePath);

		try
		{
			await Process.SetScalingAsync(VisualTreeHelper.GetDpi(this).DpiScaleX * _scaling);
			await Process.StartAsync(assemblyPath, executablePath, hostAppPath, (bool) isNetFx);
			NotifyPreviewHostRunningChanged();
			// Prefer live buffer text when available so we do not re-read disk and
			// can seed the skip-unchanged cache for subsequent edits.
			var xaml = TryGetBufferText() ?? await ReadAllTextAsync(_xamlPath);
			if (await Process.UpdateXamlAsync(xaml))
			{
				_lastSentXaml = xaml;
			}

			_buildRequired = false;
		}
		catch (ApplicationException ex)
		{
			// Don't display an error here: ProcessExited should handle that.
			Log.Logger.Debug(ex, "Process.StartAsync exited with error");
		}
		catch (FileNotFoundException ex)
		{
			_buildRequired = true;
			ShowError("Build Required", ex.Message);
			Log.Logger.Debug(ex, "StartAsync could not find executable");
		}
		catch (Exception ex)
		{
			ShowError("Error", ex.Message);
			Log.Logger.Debug(ex, "StartAsync exception");
		}
	}

	private async Task StartStopProcessAsync()
	{
		if (!_isStarted || _disposed)
		{
			return;
		}

		// Build/debug pause: stop host and show paused banner.
		if (IsPaused)
		{
			PausedMessage.Visibility = Visibility.Visible;
			StopTimer(_backgroundStopTimer);
			StopTimer(_sourceOnlyStopTimer);
			SuspendHost("designer paused");
			return;
		}

		// Tab not visible: do not run a 60 Hz host for background documents.
		if (!_isDocumentVisible)
		{
			PausedMessage.Visibility = Visibility.Collapsed;
			SuspendHost("document not visible");
			return;
		}

		PausedMessage.Visibility = Visibility.Collapsed;

		if (!Process.IsRunning && IsLoaded)
		{
			if (SelectedTarget == null)
			{
				await LoadTargetsAsync();
			}

			await StartProcessAsync();
		}

		// Source-only: host may start for initial diagnostics, then idle-suspend.
		// Error tagger keeps the last ExceptionDetails without a live process.
		if (View == AvaloniaDesignerView.Source)
		{
			ArmSourceOnlySuspendTimer();
		}
		else
		{
			StopTimer(_sourceOnlyStopTimer);
		}
	}

	/// <summary>
	/// Stops the host process without treating it as a crash. Last frame and last
	/// markup error remain available for the UI and error tagger.
	/// </summary>
	private void SuspendHost(string reason)
	{
		if (_disposed || !Process.IsRunning)
		{
			_lastSentXaml = null;
			return;
		}

		Log.Logger.Information("Suspending previewer host ({Reason})", reason);
		_hostSuspendedIntentionally = true;
		_lastSentXaml = null;
		Process.Stop();
		// Exited may notify again; call now so the tab caption drops the live marker promptly.
		NotifyPreviewHostRunningChanged();
	}

	private DispatcherTimer EnsureBackgroundStopTimer()
	{
		if (_backgroundStopTimer == null)
		{
			_backgroundStopTimer = new DispatcherTimer
			{
				Interval = BackgroundStopDelay
			};
			_backgroundStopTimer.Tick += (_, __) =>
			{
				_backgroundStopTimer.Stop();
				if (!_disposed && !_isDocumentVisible && !IsPaused)
				{
					SuspendHost("document not visible (delayed)");
				}
			};
		}

		return _backgroundStopTimer;
	}

	private void ArmSourceOnlySuspendTimer()
	{
		if (_disposed || !_isDocumentVisible || IsPaused || (View != AvaloniaDesignerView.Source))
		{
			return;
		}

		var timer = EnsureSourceOnlyStopTimer();
		timer.Stop();
		timer.Start();
	}

	private DispatcherTimer EnsureSourceOnlyStopTimer()
	{
		if (_sourceOnlyStopTimer == null)
		{
			_sourceOnlyStopTimer = new DispatcherTimer
			{
				Interval = SourceOnlyStopDelay
			};
			_sourceOnlyStopTimer.Tick += (_, __) =>
			{
				_sourceOnlyStopTimer.Stop();
				if (!_disposed &&
					_isDocumentVisible &&
					!IsPaused &&
					(View == AvaloniaDesignerView.Source) &&
					Process.IsRunning)
				{
					// Keep last error for tagger; restart on next edit via UpdateXaml.
					SuspendHost("source-only idle");
				}
			};
		}

		return _sourceOnlyStopTimer;
	}

	private static void StopTimer(DispatcherTimer timer)
	{
		timer?.Stop();
	}

	private static void StopAndDisposeTimer(ref DispatcherTimer timer)
	{
		if (timer == null)
		{
			return;
		}

		timer.Stop();
		timer = null;
	}

	private void SwapPreviewAndXamlPanes(object sender, RoutedEventArgs args)
	{
		PreviewAndXamlPanesSwapped = !PreviewAndXamlPanesSwapped;
	}

	private void TextChanged(object sender, TextContentChangedEventArgs e)
	{
		// ChangedOnBackground fires on a thread-pool thread. Snapshot text here;
		// DispatcherTimer / DependencyProperties (View) require the UI thread.
		var xaml = e.After.GetText();
		ApplyTextChangedOnUiAsync(xaml).FireAndForget();
	}

	private async Task ApplyTextChangedOnUiAsync(string xaml)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
		if (_disposed)
		{
			return;
		}

		_throttle.Interval = GetXamlDebounceInterval(xaml.Length);
		_throttle.Queue(xaml);

		// Typing in Source-only mode should keep the host alive (or restart it after idle suspend).
		if (_isDocumentVisible && !IsPaused && (View == AvaloniaDesignerView.Source))
		{
			ArmSourceOnlySuspendTimer();
		}
	}

	/// <summary>
	/// Longer idle delay for large AXAML so the host is not reloaded on every pause mid-type.
	/// </summary>
	private static TimeSpan GetXamlDebounceInterval(int length)
	{
		if (length >= XamlDebounceLargeChars)
		{
			return XamlDebounceLarge;
		}

		if (length >= XamlDebounceMediumChars)
		{
			return XamlDebounceMedium;
		}

		return XamlDebounceDefault;
	}

	/// <summary>
	/// Current editor buffer text, or null if the editor is not ready.
	/// </summary>
	private string TryGetBufferText()
	{
		try
		{
			return _editor?.TextView?.TextBuffer?.CurrentSnapshot?.GetText();
		}
		catch
		{
			return null;
		}
	}

	private void UpdateLayoutForView()
	{
		void HorizontalGrid()
		{
			if (MainGrid.RowDefinitions.Count == 0)
			{
				MainGrid.RowDefinitions.Add(PreviewRow);
				MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
				MainGrid.RowDefinitions.Add(CodeRow);
				MainGrid.ColumnDefinitions.Clear();
				Splitter.Height = 5;
				Splitter.Width = double.NaN;
			}

			Splitter.ResizeDirection = GridResizeDirection.Rows;
		}

		void VerticalGrid()
		{
			if (MainGrid.ColumnDefinitions.Count == 0)
			{
				MainGrid.ColumnDefinitions.Add(_previewCol);
				MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
				MainGrid.ColumnDefinitions.Add(_codeCol);
				MainGrid.RowDefinitions.Clear();
				Splitter.Width = 5;
				Splitter.Height = double.NaN;
			}

			Splitter.ResizeDirection = GridResizeDirection.Columns;
		}

		if (View == AvaloniaDesignerView.Split)
		{
			if (SplitOrientation == Orientation.Horizontal)
			{
				HorizontalGrid();
				var content = SwapPanesButton.Content as UIElement;
				content.RenderTransform = new RotateTransform(90);
			}
			else
			{
				VerticalGrid();
				var content = SwapPanesButton.Content as UIElement;
				content.RenderTransform = null;
			}

			// Absolute + Star (not Star/Star). See NormalizeSplitPaneSizes.
			EnsureSplitPaneSizePattern();

			Splitter.Visibility = Visibility.Visible;
			SwapPanesButton.Visibility = Visibility.Visible;
		}
		else
		{
			HorizontalGrid();
			PreviewRow.Height = View == AvaloniaDesignerView.Design ? OneStar : ZeroStar;
			CodeRow.Height = View == AvaloniaDesignerView.Source ? OneStar : ZeroStar;
			Splitter.Visibility = Visibility.Collapsed;
			SwapPanesButton.Visibility = Visibility.Collapsed;
		}

		MainGrid.UpdateLayout();

		if (View == AvaloniaDesignerView.Split)
		{
			// Actual sizes are only reliable after layout; convert equal stars if needed.
			NormalizeSplitPaneSizes();
		}
	}

	/// <summary>
	/// Ensures the two resizable panes use Absolute + Star sizing.
	/// </summary>
	/// <remarks>
	/// WPF <see cref="GridSplitter"/> uses <c>SplitBehavior.Split</c> when both definitions are
	/// star-sized. In that mode, any mid-drag change to the combined actual length of the two
	/// panes cancels the drag and restores the original lengths. Loading the Avalonia preview
	/// (frames, large scrollable margins, scrollbar layout) frequently changes that sum, which
	/// is why the splitter often works before the first frame and then gets stuck at ~50/50.
	/// Absolute + Star uses Resize1/Resize2 instead and does not cancel on sum changes.
	/// </remarks>
	private void EnsureSplitPaneSizePattern()
	{
		if (SplitOrientation == Orientation.Horizontal)
		{
			// First drag-ready pattern: pixel height on pane 0, star on pane 2.
			// If we only have stars (initial or after view toggle), leave them until
			// NormalizeSplitPaneSizes can read ActualHeight; if one is already absolute, keep it.
			if (PreviewRow.Height.IsAbsolute && CodeRow.Height.IsStar)
			{
				return;
			}

			if (CodeRow.Height.IsAbsolute && PreviewRow.Height.IsStar)
			{
				return;
			}

			// Temporary equal stars until we know pixel sizes after arrange.
			PreviewRow.Height = OneStar;
			CodeRow.Height = OneStar;
		}
		else
		{
			if (_previewCol.Width.IsAbsolute && _codeCol.Width.IsStar)
			{
				return;
			}

			if (_codeCol.Width.IsAbsolute && _previewCol.Width.IsStar)
			{
				return;
			}

			_previewCol.Width = OneStar;
			_codeCol.Width = OneStar;
		}
	}

	/// <summary>
	/// Converts star/star panes to Absolute + Star using current actual sizes (preserving ratio).
	/// </summary>
	private void NormalizeSplitPaneSizes()
	{
		if (View != AvaloniaDesignerView.Split)
		{
			return;
		}

		if (SplitOrientation == Orientation.Horizontal)
		{
			if (MainGrid.RowDefinitions.Count < 3)
			{
				return;
			}

			// Already in a splitter-safe pattern.
			if ((PreviewRow.Height.IsAbsolute && CodeRow.Height.IsStar) ||
				(CodeRow.Height.IsAbsolute && PreviewRow.Height.IsStar))
			{
				return;
			}

			var h1 = PreviewRow.ActualHeight;
			var h2 = CodeRow.ActualHeight;
			var total = h1 + h2;
			if (total <= 1)
			{
				// Layout not ready yet — try again next size change.
				MainGrid.SizeChanged -= MainGridOnSizeChangedNormalizeSplit;
				MainGrid.SizeChanged += MainGridOnSizeChangedNormalizeSplit;
				return;
			}

			// Keep the current visual ratio: first pane absolute, second fills.
			PreviewRow.Height = new GridLength(Math.Max(40, h1), GridUnitType.Pixel);
			CodeRow.Height = OneStar;
		}
		else
		{
			if (MainGrid.ColumnDefinitions.Count < 3)
			{
				return;
			}

			if ((_previewCol.Width.IsAbsolute && _codeCol.Width.IsStar) ||
				(_codeCol.Width.IsAbsolute && _previewCol.Width.IsStar))
			{
				return;
			}

			var w1 = _previewCol.ActualWidth;
			var w2 = _codeCol.ActualWidth;
			var total = w1 + w2;
			if (total <= 1)
			{
				MainGrid.SizeChanged -= MainGridOnSizeChangedNormalizeSplit;
				MainGrid.SizeChanged += MainGridOnSizeChangedNormalizeSplit;
				return;
			}

			_previewCol.Width = new GridLength(Math.Max(40, w1), GridUnitType.Pixel);
			_codeCol.Width = OneStar;
		}
	}

	private void MainGridOnSizeChangedNormalizeSplit(object sender, SizeChangedEventArgs e)
	{
		MainGrid.SizeChanged -= MainGridOnSizeChangedNormalizeSplit;
		if (View == AvaloniaDesignerView.Split)
		{
			NormalizeSplitPaneSizes();
		}
	}

	private void UpdateScaling(double scaling)
	{
		// Snap and ignore tiny DPI/float noise so we do not re-render endlessly.
		scaling = Math.Round(scaling, 2, MidpointRounding.AwayFromZero);
		if (scaling <= 0)
		{
			scaling = 1;
		}

		if (Math.Abs(scaling - _scaling) < ScalingEpsilon)
		{
			return;
		}

		_scaling = scaling;

		if (Process.IsReady)
		{
			Process.SetScalingAsync(VisualTreeHelper.GetDpi(this).DpiScaleX * _scaling).FireAndForget();
		}
	}

	private void UpdateXaml(string xaml)
	{
		PushXamlToHost(xaml, forceIncomplete: false);
	}

	/// <summary>
	/// Debounced entry for pushing buffer text to the design host.
	/// Skips clearly mid-edit markup (e.g. a lone <c>&lt;</c>) unless <paramref name="forceIncomplete"/> is set.
	/// </summary>
	private void PushXamlToHost(string xaml, bool forceIncomplete)
	{
		// Do not wake a backgrounded document's host on buffer churn from external edits.
		if (!_isDocumentVisible || IsPaused)
		{
			return;
		}

		if (Process.IsReady)
		{
			// Skip host round-trip when the settled buffer matches what we already sent.
			if (string.Equals(xaml, _lastSentXaml, StringComparison.Ordinal))
			{
				Log.Logger.Verbose("Skipping UpdateXaml; content unchanged ({Length} chars)", xaml?.Length ?? 0);
				StopTimer(_forceIncompleteSendTimer);
				_pendingForceXaml = null;
				return;
			}

			// Mid-edit (e.g. typed '<' and paused): keep last good preview; don't break host yet.
			if (!forceIncomplete && XamlEditCompleteness.IsClearlyIncomplete(xaml))
			{
				Log.Logger.Verbose("Skipping UpdateXaml; buffer looks mid-edit ({Length} chars)", xaml?.Length ?? 0);
				_pendingForceXaml = xaml;
				var forceTimer = EnsureForceIncompleteSendTimer();
				forceTimer.Stop();
				forceTimer.Start();
				return;
			}

			StopTimer(_forceIncompleteSendTimer);
			_pendingForceXaml = null;

			// Host stays alive on invalid markup; it returns UpdateXamlResult and we freeze.
			SendXamlToHostAsync(xaml).FireAndForget();

			if (View == AvaloniaDesignerView.Source)
			{
				ArmSourceOnlySuspendTimer();
			}

			return;
		}

		// Host crashed, was suspended, or exited — restart on the next edit so the user is not stuck.
		// Do not restart just to push clearly incomplete text (unless forced after long idle).
		if (_isStarted && IsLoaded && !Process.IsRunning)
		{
			if (!forceIncomplete && XamlEditCompleteness.IsClearlyIncomplete(xaml))
			{
				_pendingForceXaml = xaml;
				var forceTimer = EnsureForceIncompleteSendTimer();
				forceTimer.Stop();
				forceTimer.Start();
				return;
			}

			RestartPreviewWithXamlAsync(xaml).FireAndForget();
		}
	}

	private async Task SendXamlToHostAsync(string xaml)
	{
		try
		{
			// Cache only on successful transport; markup errors still return true
			// (host received XAML and replied with UpdateXamlResult).
			if (await Process.UpdateXamlAsync(xaml))
			{
				_lastSentXaml = xaml;
			}
		}
		catch (Exception ex)
		{
			// Unexpected hard failure — do not cache; next idle can retry the same content.
			Log.Logger.Debug(ex, "SendXamlToHostAsync failed");
		}
	}

	private async Task RestartPreviewWithXamlAsync(string xaml)
	{
		var assemblyPath = SelectedTarget?.XamlAssembly;
		var executablePath = SelectedTarget?.ExecutableAssembly;
		var hostAppPath = SelectedTarget?.HostApp;
		var isNetFx = SelectedTarget?.IsNetFramework;

		if ((assemblyPath == null) || (executablePath == null) || (hostAppPath == null) || (isNetFx == null))
		{
			return;
		}

		await _startingProcess.WaitAsync();
		try
		{
			if (IsPaused || !_isDocumentVisible || Process.IsRunning || _disposed)
			{
				return;
			}

			Log.Logger.Information("Restarting previewer after process exit");
			_firstFrame = true;
			_lastSentXaml = null;
			_hostSuspendedIntentionally = false;
			await Process.SetScalingAsync(VisualTreeHelper.GetDpi(this).DpiScaleX * _scaling);
			await Process.StartAsync(assemblyPath, executablePath, hostAppPath, (bool) isNetFx);
			NotifyPreviewHostRunningChanged();
			if (await Process.UpdateXamlAsync(xaml))
			{
				_lastSentXaml = xaml;
			}

			if (View == AvaloniaDesignerView.Source)
			{
				ArmSourceOnlySuspendTimer();
			}
		}
		catch (FileNotFoundException ex)
		{
			_buildRequired = true;
			ShowError("Build Required", ex.Message);
			Log.Logger.Debug(ex, "RestartPreview could not find executable");
		}
		catch (Exception ex)
		{
			ShowError("Preview Paused", ex.Message);
			Log.Logger.Debug(ex, "RestartPreview exception");
		}
		finally
		{
			NotifyPreviewHostRunningChanged();
			_startingProcess.Release();
		}
	}

	#endregion
}