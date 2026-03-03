#region References

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using IDispatcher = Cornerstone.Presentation.IDispatcher;

#endregion

namespace Cornerstone.Avalonia;

public abstract class CornerstoneApplication : Application, IDispatchable
{
	#region Fields

	private static CornerstoneDispatcher _dispatcher;
	private PropertyChangedEventHandler _propertyChangedHandler;

	#endregion

	#region Constructors

	protected CornerstoneApplication()
	{
		AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
		TaskScheduler.UnobservedTaskException += TaskSchedulerUnobservedTaskException;
		RuntimeInformation.Refresh();
	}

	static CornerstoneApplication()
	{
		ApplicationArguments = new ApplicationArguments();
		DependencyProvider = new DependencyProvider("Cornerstone");
		RuntimeInformation = new();
	}

	#endregion

	#region Properties

	public static ApplicationArguments ApplicationArguments { get; }

	public static DependencyProvider DependencyProvider { get; }

	public static CornerstoneDispatcher Dispatcher => _dispatcher ??= new CornerstoneDispatcher();

	public static RuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	public IDispatcher GetDispatcher()
	{
		return Dispatcher;
	}

	public static T GetInstance<T>()
	{
		return DependencyProvider.GetInstance<T>();
	}

	public static object GetInstance(Type type)
	{
		return DependencyProvider.GetInstance(type);
	}

	public static TopLevel GetTopLevel()
	{
		switch (Current?.ApplicationLifetime)
		{
			case IClassicDesktopStyleApplicationLifetime desktop:
			{
				return desktop.MainWindow!;
			}
			case ISingleViewApplicationLifetime viewApp:
			{
				var visualRoot = viewApp.MainView?.GetVisualRoot();
				return (visualRoot as TopLevel)!;
			}
			default:
			{
				return null!;
			}
		}
	}

	public static void LogException(Exception ex)
	{
		var runtimeInformation = DependencyProvider.GetInstance<IRuntimeInformation>();
		var builder = new StringBuilder();

		builder.Append("Crash: ");
		builder.AppendLine(ex.Message);
		builder.AppendLine(ex.StackTrace);

		builder.AppendLine("----------------------------");
		builder.AppendLine(runtimeInformation.ToString());
		builder.AppendLine("----------------------------");

		var directory = Path.Combine(runtimeInformation.ApplicationDataLocation, "CrashLogs");
		new DirectoryInfo(directory).SafeCreate();

		var file = Path.Combine(directory, $"Crash-{DateTime.Now.Ticks:D20}.log");
		File.WriteAllText(file, builder.ToString());
	}

	public override void OnFrameworkInitializationCompleted()
	{
		// Subscribe to dispatcher unhandled exceptions
		global::Avalonia.Threading.Dispatcher.UIThread.UnhandledException += OnDispatcherOnUnhandledException;
		base.OnFrameworkInitializationCompleted();
	}

	public override void RegisterServices()
	{
		DependencyProvider.AddSingleton(ApplicationArguments);
		DependencyProvider.SetupCornerstoneDependencies(
			dispatcher: Dispatcher,
			runtimeInformation: RuntimeInformation
		);

		base.RegisterServices();
	}

	public static async Task<string> TryOpenFileAsync(
		string startingDirectory = null,
		params FilePickerFileType[] pickerTypes)
	{
		var topLevel = GetTopLevel();
		if (topLevel == null)
		{
			return null;
		}

		if (string.IsNullOrWhiteSpace(startingDirectory))
		{
			startingDirectory = RuntimeInformation.ApplicationDataLocation;
		}

		var defaultDirectory = await topLevel.StorageProvider.TryGetFolderFromPathAsync(startingDirectory);
		var options = new FilePickerOpenOptions
		{
			AllowMultiple = false,
			SuggestedStartLocation = defaultDirectory,
			FileTypeFilter = pickerTypes
		};

		var selected = await topLevel.StorageProvider.OpenFilePickerAsync(options);
		return selected.Count == 1 ? selected[0].Path.LocalPath : null;
	}

	public static async Task<string> TrySelectFileForSave(
		string startingDirectory = null,
		params FilePickerFileType[] fileTypeChoices)
	{
		var topLevel = GetTopLevel();
		if (topLevel == null)
		{
			return null;
		}

		startingDirectory ??= RuntimeInformation.ApplicationDataLocation;

		var defaultDirectory = await topLevel.StorageProvider.TryGetFolderFromPathAsync(startingDirectory);
		var options = new FilePickerSaveOptions
		{
			SuggestedStartLocation = defaultDirectory,
			FileTypeChoices = fileTypeChoices
		};

		var selected = await topLevel.StorageProvider.SaveFilePickerAsync(options);
		return selected?.TryGetLocalPath();
	}

	public static async Task<string> TrySelectFolderAsync(string startingDirectory = null)
	{
		var topLevel = GetTopLevel();
		if (topLevel == null)
		{
			return null;
		}

		var options = new FolderPickerOpenOptions { AllowMultiple = false, Title = "Select Folder" };
		var selected = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
		var path = selected!.FirstOrDefault();
		var response = path.TryGetLocalPath() ?? path.Path.ToString();
		return response;
	}

	protected void OnPropertyChanged(string propertyName)
	{
		_propertyChangedHandler ??= AvaloniaExtensions.GetPropertyChangedHandler(this);
		_propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		LogException(e.ExceptionObject as Exception);
	}

	private void OnDispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		LogException(e.Exception);
	}

	private void TaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
	{
		if (e.Exception.InnerException is { Message: "Looping animations must not use the Run method." })
		{
			// Ignore this but would be nice to fix it.
			return;
		}

		LogException(e.Exception);
	}

	#endregion
}