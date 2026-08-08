#region References

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Cornerstone.VisualStudio.Services;
using Cornerstone.VisualStudio.Views;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
using Task = System.Threading.Tasks.Task;

#endregion

namespace Cornerstone.VisualStudio.Extensibility;

/// <summary>
/// Applies modern Visual Studio Settings (Tools → Options) into MEF <see cref="ICornerstoneSettings"/>.
/// </summary>
/// <remarks>
/// VS 2026 Options writes extension settings to the instance settings.json
/// (e.g. cornerstone.designerView). The VisualStudio.Extensibility Read/Subscribe
/// APIs have been unreliable from the hybrid VSSDK package context, so we treat
/// settings.json as the source of truth for applying values into the runtime store.
/// </remarks>
internal static class CornerstoneSettingsBridge
{
	#region Constants

	private const string KeyDesignerView = "cornerstone.designerView";
	private const string KeySplitOrientation = "cornerstone.designerSplitOrientation";
	private const string KeySplitSwapped = "cornerstone.designerSplitSwapped";
	private const string KeyZoomLevel = "cornerstone.zoomLevel";
	private const string KeyLogVerbosity = "cornerstone.minimumLogVerbosity";
	private const string KeyTabPrefix = "cornerstone.showPreviewHostRunningInTab";

	#endregion

	#region Fields

	private static int _started;
	private static FileSystemWatcher _watcher;
	private static ICornerstoneSettings _store;
	private static string _settingsJsonPath;
	private static readonly object Sync = new();

	#endregion

	#region Methods

	/// <summary>
	/// Starts watching modern Settings and applies them into the MEF store.
	/// </summary>
	public static async Task StartAsync(AsyncPackage package, CancellationToken cancellationToken)
	{
		if (Interlocked.Exchange(ref _started, 1) != 0)
		{
			return;
		}

		try
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			ICornerstoneSettings store;
			try
			{
				store = package.GetMefService<ICornerstoneSettings>();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to resolve ICornerstoneSettings for Settings bridge");
				Interlocked.Exchange(ref _started, 0);
				return;
			}

			_store = store;

			if (!TryGetSettingsJsonPath(out var path))
			{
				Log.Warning("Could not resolve Visual Studio settings.json path; modern Settings will not apply");
				// Still allow manual pulls if path appears later.
			}
			else
			{
				_settingsJsonPath = path;
				Log.Information("Modern Settings file: {Path}", path);
			}

			// Apply current Options values into MEF immediately.
			PullFromExtensibilityBlocking(store);

			if (!string.IsNullOrEmpty(_settingsJsonPath) && File.Exists(_settingsJsonPath))
			{
				StartFileWatcher(_settingsJsonPath);
			}

			Log.Information(
				"Cornerstone Settings bridge started (View={View}, Orientation={Orientation}, Swapped={Swapped}, Zoom={Zoom}, Log={Log}, TabPrefix={Prefix})",
				store.DesignerView,
				store.DesignerSplitOrientation,
				store.DesignerSplitSwapped,
				store.ZoomLevel,
				store.MinimumLogVerbosity,
				store.ShowPreviewHostRunningInTab);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to start Cornerstone Settings bridge");
			Interlocked.Exchange(ref _started, 0);
		}
	}

	/// <summary>
	/// Reads current modern Settings into the MEF store (safe to call from the UI thread).
	/// </summary>
	public static void PullFromExtensibilityBlocking(ICornerstoneSettings store)
	{
		if (store is null)
		{
			return;
		}

		try
		{
			ThreadHelper.ThrowIfNotOnUIThread();
		}
		catch
		{
			// If not on UI thread, marshal.
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				ApplyFromSettingsJson(store);
			});
			return;
		}

		ApplyFromSettingsJson(store);
	}

	/// <summary>
	/// Async pull for call sites that already have a joinable context.
	/// </summary>
	public static async Task PullFromExtensibilityAsync(
		ICornerstoneSettings store,
		CancellationToken cancellationToken)
	{
		if (store is null)
		{
			return;
		}

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
		ApplyFromSettingsJson(store);
	}

	private static void ApplyFromSettingsJson(ICornerstoneSettings store)
	{
		lock (Sync)
		{
			try
			{
				if (string.IsNullOrEmpty(_settingsJsonPath) && !TryGetSettingsJsonPath(out _settingsJsonPath))
				{
					Log.Debug("settings.json not found; leaving MEF store unchanged");
					return;
				}

				if (!File.Exists(_settingsJsonPath))
				{
					Log.Debug("settings.json missing at {Path}", _settingsJsonPath);
					return;
				}

				// Read with share so VS can keep the file open.
				string text;
				using (var stream = new FileStream(
							_settingsJsonPath,
							FileMode.Open,
							FileAccess.Read,
							FileShare.ReadWrite | FileShare.Delete))
				using (var reader = new StreamReader(stream))
				{
					text = reader.ReadToEnd();
				}

				// File may start with a /* comment */ header.
				var jsonStart = text.IndexOf('{');
				if (jsonStart < 0)
				{
					Log.Warning("settings.json does not contain a JSON object");
					return;
				}

				var root = JObject.Parse(text.Substring(jsonStart));

				if (TryGetString(root, KeyDesignerView, out var viewText)
					&& Enum.TryParse(viewText, ignoreCase: true, out AvaloniaDesignerView view))
				{
					store.DesignerView = view;
				}

				if (TryGetString(root, KeySplitOrientation, out var orientationText)
					&& Enum.TryParse(orientationText, ignoreCase: true, out Orientation orientation))
				{
					store.DesignerSplitOrientation = orientation;
				}

				if (TryGetBool(root, KeySplitSwapped, out var swapped))
				{
					store.DesignerSplitSwapped = swapped;
				}

				if (TryGetString(root, KeyZoomLevel, out var zoom) && !string.IsNullOrWhiteSpace(zoom))
				{
					store.ZoomLevel = zoom;
				}

				if (TryGetString(root, KeyLogVerbosity, out var logText)
					&& Enum.TryParse(logText, ignoreCase: true, out LogEventLevel level))
				{
					store.MinimumLogVerbosity = level;
				}

				if (TryGetBool(root, KeyTabPrefix, out var tabPrefix))
				{
					store.ShowPreviewHostRunningInTab = tabPrefix;
				}

				store.Save();

				Log.Information(
					"Applied modern Settings from JSON: View={View}, Orientation={Orientation}, Swapped={Swapped}, Zoom={Zoom}, Log={Log}, TabPrefix={Prefix}",
					store.DesignerView,
					store.DesignerSplitOrientation,
					store.DesignerSplitSwapped,
					store.ZoomLevel,
					store.MinimumLogVerbosity,
					store.ShowPreviewHostRunningInTab);
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "Failed to apply modern Settings from settings.json");
			}
		}
	}

	private static bool TryGetString(JObject root, string key, out string value)
	{
		value = null;
		var token = root[key];
		if (token is null || token.Type == JTokenType.Null)
		{
			return false;
		}

		value = token.Type == JTokenType.String ? token.Value<string>() : token.ToString();
		return !string.IsNullOrEmpty(value);
	}

	private static bool TryGetBool(JObject root, string key, out bool value)
	{
		value = false;
		var token = root[key];
		if (token is null || token.Type == JTokenType.Null)
		{
			return false;
		}

		if (token.Type == JTokenType.Boolean)
		{
			value = token.Value<bool>();
			return true;
		}

		return bool.TryParse(token.ToString(), out value);
	}

	private static bool TryGetSettingsJsonPath(out string path)
	{
		path = null;
		ThreadHelper.ThrowIfNotOnUIThread();

		try
		{
			var shell = Package.GetGlobalService(typeof(SVsShell)) as IVsShell;
			if (shell is null)
			{
				return false;
			}

			// e.g. Software\Microsoft\VisualStudio\18.0_642f1c90Exp
			if (ErrorHandler.Failed(shell.GetProperty((int) __VSSPROPID.VSSPROPID_VirtualRegistryRoot, out var rootObj))
				|| rootObj is not string root
				|| string.IsNullOrEmpty(root))
			{
				return false;
			}

			var hive = root;
			var slash = root.LastIndexOf('\\');
			if (slash >= 0 && slash < root.Length - 1)
			{
				hive = root.Substring(slash + 1);
			}

			path = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Microsoft",
				"VisualStudio",
				hive,
				"settings.json");

			return true;
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "TryGetSettingsJsonPath failed");
			return false;
		}
	}

	private static void StartFileWatcher(string settingsJsonPath)
	{
		try
		{
			_watcher?.Dispose();
			var directory = Path.GetDirectoryName(settingsJsonPath);
			var fileName = Path.GetFileName(settingsJsonPath);
			if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
			{
				return;
			}

			_watcher = new FileSystemWatcher(directory, fileName)
			{
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
				IncludeSubdirectories = false,
				EnableRaisingEvents = true
			};

			FileSystemEventHandler handler = (_, _) => OnSettingsFileChanged();
			RenamedEventHandler renamed = (_, _) => OnSettingsFileChanged();
			_watcher.Changed += handler;
			_watcher.Created += handler;
			_watcher.Renamed += renamed;

			Log.Debug("Watching modern Settings file for changes");
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to watch settings.json for live updates");
		}
	}

	private static void OnSettingsFileChanged()
	{
		// FileSystemWatcher callback is not async; schedule apply on the joinable context.
		// Use Task.FireAndForget (not discarded RunAsync) so faults are logged and VSSDK007 is satisfied.
		ApplySettingsFileChangeAsync().FireAndForget();
	}

	private static async Task ApplySettingsFileChangeAsync()
	{
		try
		{
			// Debounce: VS may write the file multiple times in a row.
			await Task.Delay(200);
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
			var store = _store;
			if (store is null)
			{
				return;
			}

			ApplyFromSettingsJson(store);
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Settings file change apply failed");
		}
	}

	#endregion
}