#region References

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Agent.Tools;

public class WriteFileTool : AgentTool
{
	#region Constructors

	public WriteFileTool(Keystone.State.AppSettings settings) : base(settings)
	{
	}

	#endregion

	#region Properties

	public override string Description => "Writes or overwrites text content to a local file (requires user confirmation).";

	public override string Name => "FileWrite";

	public override string ParametersJsonSchema =>
		"""
		{
		  "type": "object",
		  "properties": {
		    "path": { "type": "string", "description": "Absolute path to the destination file" },
		    "content": { "type": "string", "description": "The text content to write" }
		  },
		  "required": ["path", "content"]
		}
		""";

	#endregion

	#region Methods

	public override Task<ToolResult> ExecuteAsync(PartialUpdate parameters, CancellationToken ct)
	{
		var filePath = parameters.Get<string>("path");
		var content = parameters.Get<string>("content");

		if (string.IsNullOrEmpty(filePath))
		{
			return Task.FromResult(ToolResult.AsError("Missing or invalid 'path' parameter."));
		}

		if (content == null)
		{
			return Task.FromResult(ToolResult.AsError("Missing or invalid 'content' parameter."));
		}

		if (!ValidatePath(filePath, out var fileInfo))
		{
			return Task.FromResult(ToolResult.AsError("Error: Path is invalid or outside allowed directory."));
		}

		try
		{
			var fullPath = Path.GetFullPath(filePath);
			var windowsDir = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows));

			if (fullPath.StartsWith(windowsDir, StringComparison.OrdinalIgnoreCase))
			{
				return Task.FromResult(ToolResult.AsError("Access denied: Writing to Windows system directories is blocked."));
			}

			// Create directories if not existing
			var dir = Path.GetDirectoryName(fullPath);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			File.WriteAllText(fullPath, content);
			return Task.FromResult(ToolResult.AsSuccess($"File written successfully to {fullPath}"));
		}
		catch (Exception ex)
		{
			return Task.FromResult(ToolResult.AsError($"Failed to write file: {ex.Message}"));
		}
	}

	#endregion
}