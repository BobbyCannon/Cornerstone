#region References

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Agent.Tools;

public abstract class AgentTool
{
	#region Constants

	public const int MaxFilesToScan = 10000;
	public const int MaxSearchDepth = 1000;
	public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
	public const long MaxRequestBodyBytes = 1 * 1024 * 1024; // 1MB

	#endregion

	#region Constructors

	protected AgentTool(Keystone.State.AppSettings settings)
	{
		Settings = settings;
	}

	#endregion

	#region Properties

	public abstract string Description { get; }
	public abstract string Name { get; }
	public abstract string ParametersJsonSchema { get; }
	public virtual bool RequiresConfirmation => false;
	public Keystone.State.AppSettings Settings { get; }

	#endregion

	#region Methods

	public abstract Task<ToolResult> ExecuteAsync(PartialUpdate properties, CancellationToken ct);

	public string GetConfirmationDetails(PartialUpdate properties)
	{
		return string.Empty;
	}

	protected bool ValidatePath(string requestedPath, out FileInfo fileInfo)
	{
		fileInfo = null;
		if (string.IsNullOrWhiteSpace(requestedPath))
		{
			return false;
		}

		try
		{
			var fullPath = Path.GetFullPath(requestedPath);
			var canonicalPath = fullPath;
			var fileInfoObject = new FileInfo(fullPath);

			if (fileInfoObject.Exists && !string.IsNullOrEmpty(fileInfoObject.LinkTarget))
			{
				var target = fileInfoObject.ResolveLinkTarget(true);
				if (target != null)
				{
					canonicalPath = target.FullName;
				}
			}

			if ((Settings.AllowedDirectories == null) || (Settings.AllowedDirectories.Count == 0))
			{
				return false;
			}

			foreach (var allowedDir in Settings.AllowedDirectories)
			{
				if (string.IsNullOrWhiteSpace(allowedDir))
				{
					continue;
				}

				var absoluteAllowedDir = Path.GetFullPath(allowedDir);
				if (!absoluteAllowedDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
				{
					absoluteAllowedDir += Path.DirectorySeparatorChar;
				}

				if (canonicalPath.StartsWith(absoluteAllowedDir, StringComparison.OrdinalIgnoreCase))
				{
					fileInfo = new FileInfo(fullPath);
					return true;
				}
			}
		}
		catch
		{
			return false;
		}

		return false;
	}

	#endregion
}