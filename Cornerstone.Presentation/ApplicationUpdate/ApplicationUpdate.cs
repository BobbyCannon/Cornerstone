#region References

using System;
using System.Text.RegularExpressions;
using Cornerstone.FileSystem.Sync;
using Cornerstone.Runtime;
#if WINDOWS
using System.Linq;
using Cornerstone.Platforms.Windows;
#endif

#endregion

namespace Cornerstone.Presentation.ApplicationUpdate;

/// <summary>
/// Copies the running application directory onto a <c> -Update </c> destination.
/// </summary>
public static class ApplicationUpdate
{
	#region Constants

	/// <summary>
	/// Argument whose value is the install folder. Example: <c> -Update C:\Data\Software\App </c>.
	/// </summary>
	public const string ArgumentName = "Update";

	/// <summary>
	/// When present, the update window waits until a debugger is attached.
	/// </summary>
	public const string DebuggerArgumentName = "Debugger";

	/// <summary>
	/// Right-only files plus folders above this count abort the copy.
	/// </summary>
	public const int MaximumRemovals = 100;

	#endregion

	#region Fields

	private static readonly Regex ProtectedDirectoriesRegex;
	private static readonly Regex ProtectedRootDirectoriesRegex;

	#endregion

	#region Constructors

	static ApplicationUpdate()
	{
		ProtectedDirectoriesRegex = new(
			@"^(?:[a-zA-Z]:\\(?:$|Windows(?:\\.*)?$|Program Files(?:\\.*)?$|Program Files \(x86\)(?:\\.*)?$|Users(?:\\.*)?$|System Volume Information(?:\\.*)?$|ProgramData(?:\\.*)?$))",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		ProtectedRootDirectoriesRegex = new("^(?:[a-zA-Z]:)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Status text for a result that has no exception message.
	/// </summary>
	public static string GetStatus(ApplicationUpdateResult result)
	{
		return result switch
		{
			ApplicationUpdateResult.Success => "done",
			ApplicationUpdateResult.DestinationMissing => "Destination is not provided.",
			ApplicationUpdateResult.ProtectedDestination => "Destination is not allowed.",
			ApplicationUpdateResult.DestinationNotApplication => "Invalid destination. Check the destination.",
			ApplicationUpdateResult.TooManyRemovals => "Too many items to remove. Check the destination.",
			_ => "Update failed."
		};
	}

	/// <summary>
	/// True for a drive root and for Windows, Program Files, Users, ProgramData, and System Volume Information.
	/// </summary>
	public static bool IsProtectedDestination(string destination)
	{
		if (string.IsNullOrWhiteSpace(destination))
		{
			return false;
		}

		return ProtectedDirectoriesRegex.IsMatch(destination)
			|| ProtectedRootDirectoriesRegex.IsMatch(destination);
	}

	/// <summary>
	/// True when <paramref name="arguments" /> carries <see cref="ArgumentName" /> and a destination path.
	/// </summary>
	public static bool IsRequested(ApplicationArguments arguments)
	{
		if ((arguments == null) || !arguments.Exists(ArgumentName))
		{
			return false;
		}

		return !string.IsNullOrWhiteSpace(arguments[ArgumentName]);
	}

	/// <summary>
	/// Closes other processes of this application whose path is under <paramref name="destination" />.
	/// On non-Windows targets this does nothing.
	/// </summary>
	public static void ShutdownDestinationProcesses(string destination)
	{
		if (string.IsNullOrWhiteSpace(destination))
		{
			return;
		}

		#if WINDOWS
		using var myProcess = ProcessService.GetCurrentProcess();
		if ((myProcess == null) || string.IsNullOrWhiteSpace(myProcess.Name))
		{
			return;
		}

		var currentPath = myProcess.FilePath;
		var processes = ProcessService.Where(myProcess.Name)
			.Where(process =>
				(process.FilePath != null)
				&& process.FilePath.StartsWith(destination, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(process.FilePath, currentPath, StringComparison.OrdinalIgnoreCase))
			.ToList();

		foreach (var process in processes)
		{
			using (process)
			{
				process.Close(3000);
				if (process.Process is { HasExited: false })
				{
					process.Kill(2000);
				}
			}
		}
		#endif
	}

	/// <summary>
	/// Copies <paramref name="source" /> onto <paramref name="destination" /> when the destination is
	/// empty or already contains <paramref name="applicationFileName" />.
	/// </summary>
	public static ApplicationUpdateResult TryApply(
		string source,
		string destination,
		string applicationFileName,
		IProgress<ApplicationUpdateProgress> progress)
	{
		if (string.IsNullOrWhiteSpace(destination))
		{
			Report(progress, 100, ApplicationUpdateResult.DestinationMissing);
			return ApplicationUpdateResult.DestinationMissing;
		}

		if (IsProtectedDestination(destination))
		{
			Report(progress, 100, ApplicationUpdateResult.ProtectedDestination);
			return ApplicationUpdateResult.ProtectedDestination;
		}

		if (string.IsNullOrWhiteSpace(applicationFileName) || string.IsNullOrWhiteSpace(source))
		{
			Report(progress, 100, ApplicationUpdateResult.Failed);
			return ApplicationUpdateResult.Failed;
		}

		try
		{
			Report(progress, 50, "processing location");
			var log = new SyncLog();
			var settings = new FileSyncSettings
			{
				CopyEmptyDirectories = false,
				CopyLeftOnlyFiles = true,
				DeleteRightOnlyDirectories = true,
				DeleteRightOnlyFiles = true,
				Recursive = true
			};
			var differences = FileSync.Compare(log, source, destination, settings);
			if (!ContainsApplication(differences, applicationFileName))
			{
				Report(progress, 100, ApplicationUpdateResult.DestinationNotApplication);
				return ApplicationUpdateResult.DestinationNotApplication;
			}

			var removals = differences.RightOnlyFiles.Count + differences.RightOnlyFolders.Count;
			if (removals > MaximumRemovals)
			{
				Report(progress, 100, ApplicationUpdateResult.TooManyRemovals);
				return ApplicationUpdateResult.TooManyRemovals;
			}

			Report(progress, 75, "updating location");
			FileSync.Process(log, differences, source, destination, settings);
			Report(progress, 100, ApplicationUpdateResult.Success);
			return ApplicationUpdateResult.Success;
		}
		catch (Exception ex)
		{
			progress?.Report(new ApplicationUpdateProgress(100, ex.Message));
			return ApplicationUpdateResult.Failed;
		}
	}

	private static bool ContainsApplication(DifferenceResults differences, string applicationFileName)
	{
		if (differences.RightFiles.Count == 0)
		{
			return true;
		}

		var windowsSuffix = "\\" + applicationFileName;
		var unixSuffix = "/" + applicationFileName;
		foreach (var file in differences.RightFiles)
		{
			if (file.Equals(applicationFileName, StringComparison.OrdinalIgnoreCase)
				|| file.EndsWith(windowsSuffix, StringComparison.OrdinalIgnoreCase)
				|| file.EndsWith(unixSuffix, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static void Report(IProgress<ApplicationUpdateProgress> progress, int percent, ApplicationUpdateResult result)
	{
		progress?.Report(new ApplicationUpdateProgress(percent, GetStatus(result)));
	}

	private static void Report(IProgress<ApplicationUpdateProgress> progress, int percent, string status)
	{
		progress?.Report(new ApplicationUpdateProgress(percent, status));
	}

	#endregion
}