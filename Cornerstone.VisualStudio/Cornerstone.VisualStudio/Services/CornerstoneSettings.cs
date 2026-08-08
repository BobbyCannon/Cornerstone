#region References

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Cornerstone.VisualStudio.Core.Cleanup;
using Cornerstone.VisualStudio.Views;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Serilog;
using Serilog.Events;

#endregion

namespace Cornerstone.VisualStudio.Services;

[Export(typeof(ICornerstoneSettings))]
public class CornerstoneSettings : ICornerstoneSettings
{
	#region Constants

	private const string SettingsKey = nameof(CornerstoneSettings);

	#endregion

	#region Fields

	private bool _cleanupEnsureFinalNewline = true;
	private string _cleanupFileExtensions = CleanupOptions.DefaultFileExtensions;
	private bool _cleanupFormatXml = true;
	private int _cleanupIndentSize = CleanupOptions.DefaultIndentSize;
	private CleanupLineEndingMode _cleanupNormalizeLineEndings = CleanupLineEndingMode.Keep;
	private bool _cleanupPreferSelfClosing = true;
	private bool _cleanupSortAttributes = true;
	private bool _cleanupSortXmlns = true;
	private bool _cleanupTrimTrailingWhitespace = true;
	private Orientation _designerSplitOrientation;
	private bool _designerSplitSwapped;
	private AvaloniaDesignerView _designerView;
	private LogEventLevel _minimumLogVerbosity;
	private bool _modernSettingsSeeded;
	private readonly WritableSettingsStore _settings;
	private bool _showPreviewHostRunningInTab;
	private bool _usageTracking;
	private string _zoomLevel;

	#endregion

	#region Constructors

	[ImportingConstructor]
	public CornerstoneSettings(SVsServiceProvider vsServiceProvider)
	{
		var shellSettingsManager = new ShellSettingsManager(vsServiceProvider);
		_designerView = AvaloniaDesignerView.Split;
		_minimumLogVerbosity = LogEventLevel.Information;
		_settings = shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
		Load();
	}

	#endregion

	#region Properties

	public bool CleanupEnsureFinalNewline
	{
		get => _cleanupEnsureFinalNewline;
		set => SetField(ref _cleanupEnsureFinalNewline, value);
	}

	public string CleanupFileExtensions
	{
		get => _cleanupFileExtensions;
		set => SetField(ref _cleanupFileExtensions, string.IsNullOrWhiteSpace(value)
			? CleanupOptions.DefaultFileExtensions
			: value.Trim());
	}

	public bool CleanupFormatXml
	{
		get => _cleanupFormatXml;
		set => SetField(ref _cleanupFormatXml, value);
	}

	public int CleanupIndentSize
	{
		get => _cleanupIndentSize;
		set => SetField(ref _cleanupIndentSize, value < 0 ? 0 : value);
	}

	public CleanupLineEndingMode CleanupNormalizeLineEndings
	{
		get => _cleanupNormalizeLineEndings;
		set => SetField(ref _cleanupNormalizeLineEndings, value);
	}

	public bool CleanupPreferSelfClosing
	{
		get => _cleanupPreferSelfClosing;
		set => SetField(ref _cleanupPreferSelfClosing, value);
	}

	public bool CleanupSortAttributes
	{
		get => _cleanupSortAttributes;
		set => SetField(ref _cleanupSortAttributes, value);
	}

	public bool CleanupSortXmlns
	{
		get => _cleanupSortXmlns;
		set => SetField(ref _cleanupSortXmlns, value);
	}

	public bool CleanupTrimTrailingWhitespace
	{
		get => _cleanupTrimTrailingWhitespace;
		set => SetField(ref _cleanupTrimTrailingWhitespace, value);
	}

	public Orientation DesignerSplitOrientation
	{
		get => _designerSplitOrientation;
		set => SetField(ref _designerSplitOrientation, value);
	}

	public bool DesignerSplitSwapped
	{
		get => _designerSplitSwapped;
		set => SetField(ref _designerSplitSwapped, value);
	}

	public AvaloniaDesignerView DesignerView
	{
		get => _designerView;
		set => SetField(ref _designerView, value);
	}

	public LogEventLevel MinimumLogVerbosity
	{
		get => _minimumLogVerbosity;
		set => SetField(ref _minimumLogVerbosity, value);
	}

	/// <summary>
	/// When true, document tabs for open AXAML designers are prefixed with "•" while the previewer host is running.
	/// Default is false (opt-in for diagnostics).
	/// </summary>
	public bool ShowPreviewHostRunningInTab
	{
		get => _showPreviewHostRunningInTab;
		set => SetField(ref _showPreviewHostRunningInTab, value);
	}

	/// <summary>
	/// True after legacy WritableSettingsStore values were copied into modern Extensibility Settings once.
	/// </summary>
	public bool ModernSettingsSeeded
	{
		get => _modernSettingsSeeded;
		set => SetField(ref _modernSettingsSeeded, value);
	}

	public bool UsageTracking
	{
		get => _usageTracking;
		set => SetField(ref _usageTracking, value);
	}

	public string ZoomLevel
	{
		get => _zoomLevel;
		set
		{
			var normalized = NormalizeZoomLevel(value);
			if (_zoomLevel != normalized)
			{
				_zoomLevel = normalized;
				RaisePropertyChanged();
			}
		}
	}

	/// <summary>
	/// Fit All / Fit to Width were removed; coerce legacy values to 100%.
	/// </summary>
	private static string NormalizeZoomLevel(string zoomLevel)
	{
		if (string.IsNullOrWhiteSpace(zoomLevel))
		{
			return "100%";
		}

		if (zoomLevel.StartsWith("Fit", StringComparison.OrdinalIgnoreCase))
		{
			return "100%";
		}

		return zoomLevel;
	}

	#endregion

	#region Methods

	public CleanupOptions CreateCleanupOptions()
	{
		return new CleanupOptions
		{
			FileExtensions = CleanupFileExtensions,
			TrimTrailingWhitespace = CleanupTrimTrailingWhitespace,
			EnsureFinalNewline = CleanupEnsureFinalNewline,
			NormalizeLineEndings = CleanupNormalizeLineEndings,
			FormatXml = CleanupFormatXml,
			SortXmlns = CleanupSortXmlns,
			SortAttributes = CleanupSortAttributes,
			PreferSelfClosing = CleanupPreferSelfClosing,
			IndentSize = CleanupIndentSize
		};
	}

	public void Load()
	{
		try
		{
			DesignerSplitOrientation = (Orientation) _settings.GetInt32(
				SettingsKey,
				nameof(DesignerSplitOrientation),
				(int) Orientation.Vertical);
			DesignerSplitSwapped = _settings.GetBoolean(
				SettingsKey,
				nameof(DesignerSplitSwapped),
				false);
			DesignerView = (AvaloniaDesignerView) _settings.GetInt32(
				SettingsKey,
				nameof(DesignerView),
				(int) AvaloniaDesignerView.Split);
			MinimumLogVerbosity = (LogEventLevel) _settings.GetInt32(
				SettingsKey,
				nameof(MinimumLogVerbosity),
				(int) LogEventLevel.Information);
			ShowPreviewHostRunningInTab = _settings.GetBoolean(
				SettingsKey,
				nameof(ShowPreviewHostRunningInTab),
				false);
			ModernSettingsSeeded = _settings.GetBoolean(
				SettingsKey,
				nameof(ModernSettingsSeeded),
				false);
			ZoomLevel = NormalizeZoomLevel(
				_settings.GetString(
					SettingsKey,
					nameof(ZoomLevel),
					"100%"));
			UsageTracking = _settings.GetBoolean(
				SettingsKey,
				nameof(UsageTracking),
				true);

			CleanupFileExtensions = _settings.GetString(
				SettingsKey,
				nameof(CleanupFileExtensions),
				CleanupOptions.DefaultFileExtensions);
			CleanupTrimTrailingWhitespace = _settings.GetBoolean(
				SettingsKey,
				nameof(CleanupTrimTrailingWhitespace),
				true);
			CleanupEnsureFinalNewline = _settings.GetBoolean(
				SettingsKey,
				nameof(CleanupEnsureFinalNewline),
				true);
			CleanupNormalizeLineEndings = (CleanupLineEndingMode) _settings.GetInt32(
				SettingsKey,
				nameof(CleanupNormalizeLineEndings),
				(int) CleanupLineEndingMode.Keep);
			CleanupFormatXml = _settings.GetBoolean(
				SettingsKey,
				nameof(CleanupFormatXml),
				true);
			CleanupSortXmlns = _settings.GetBoolean(
				SettingsKey,
				nameof(CleanupSortXmlns),
				true);
			CleanupSortAttributes = _settings.GetBoolean(
				SettingsKey,
				nameof(CleanupSortAttributes),
				true);
			CleanupPreferSelfClosing = _settings.GetBoolean(
				SettingsKey,
				nameof(CleanupPreferSelfClosing),
				true);
			CleanupIndentSize = _settings.GetInt32(
				SettingsKey,
				nameof(CleanupIndentSize),
				CleanupOptions.DefaultIndentSize);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to load settings");
		}
	}

	public void Save()
	{
		try
		{
			if (!_settings.CollectionExists(SettingsKey))
			{
				_settings.CreateCollection(SettingsKey);
			}

			_settings.SetInt32(SettingsKey, nameof(DesignerSplitOrientation), (int) DesignerSplitOrientation);
			_settings.SetBoolean(SettingsKey, nameof(DesignerSplitSwapped), DesignerSplitSwapped);
			_settings.SetInt32(SettingsKey, nameof(DesignerView), (int) DesignerView);
			_settings.SetInt32(SettingsKey, nameof(MinimumLogVerbosity), (int) MinimumLogVerbosity);
			_settings.SetBoolean(SettingsKey, nameof(ShowPreviewHostRunningInTab), ShowPreviewHostRunningInTab);
			_settings.SetBoolean(SettingsKey, nameof(ModernSettingsSeeded), ModernSettingsSeeded);
			_settings.SetString(SettingsKey, nameof(ZoomLevel), ZoomLevel);
			_settings.SetBoolean(SettingsKey, nameof(UsageTracking), UsageTracking);

			_settings.SetString(SettingsKey, nameof(CleanupFileExtensions), CleanupFileExtensions);
			_settings.SetBoolean(SettingsKey, nameof(CleanupTrimTrailingWhitespace), CleanupTrimTrailingWhitespace);
			_settings.SetBoolean(SettingsKey, nameof(CleanupEnsureFinalNewline), CleanupEnsureFinalNewline);
			_settings.SetInt32(SettingsKey, nameof(CleanupNormalizeLineEndings), (int) CleanupNormalizeLineEndings);
			_settings.SetBoolean(SettingsKey, nameof(CleanupFormatXml), CleanupFormatXml);
			_settings.SetBoolean(SettingsKey, nameof(CleanupSortXmlns), CleanupSortXmlns);
			_settings.SetBoolean(SettingsKey, nameof(CleanupSortAttributes), CleanupSortAttributes);
			_settings.SetBoolean(SettingsKey, nameof(CleanupPreferSelfClosing), CleanupPreferSelfClosing);
			_settings.SetInt32(SettingsKey, nameof(CleanupIndentSize), CleanupIndentSize);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to save settings");
		}
	}

	private void RaisePropertyChanged([CallerMemberName] string propName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
	}

	private void SetField<T>(ref T field, T value, [CallerMemberName] string propName = null)
	{
		if (Equals(field, value))
		{
			return;
		}

		field = value;
		RaisePropertyChanged(propName);
	}

	#endregion

	#region Events

	public event PropertyChangedEventHandler PropertyChanged;

	#endregion
}

public interface ICornerstoneSettings : INotifyPropertyChanged
{
	#region Properties

	bool CleanupEnsureFinalNewline { get; set; }
	string CleanupFileExtensions { get; set; }
	bool CleanupFormatXml { get; set; }
	int CleanupIndentSize { get; set; }
	CleanupLineEndingMode CleanupNormalizeLineEndings { get; set; }
	bool CleanupPreferSelfClosing { get; set; }
	bool CleanupSortAttributes { get; set; }
	bool CleanupSortXmlns { get; set; }
	bool CleanupTrimTrailingWhitespace { get; set; }
	Orientation DesignerSplitOrientation { get; set; }
	bool DesignerSplitSwapped { get; set; }
	AvaloniaDesignerView DesignerView { get; set; }
	LogEventLevel MinimumLogVerbosity { get; set; }
	/// <summary>
	/// When true, document tabs are prefixed with "•" while the previewer host is running.
	/// </summary>
	bool ShowPreviewHostRunningInTab { get; set; }
	/// <summary>
	/// True after legacy store values were copied into modern Extensibility Settings once.
	/// </summary>
	bool ModernSettingsSeeded { get; set; }
	bool UsageTracking { get; set; }
	string ZoomLevel { get; set; }

	#endregion

	#region Methods

	CleanupOptions CreateCleanupOptions();
	void Load();
	void Save();

	#endregion
}