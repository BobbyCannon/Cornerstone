#region References

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Agent.Tools;

public class ReadFileTool : AgentTool
{
	#region Constructors

	public ReadFileTool(Keystone.State.AppSettings appSettings) : base(appSettings)
	{
	}

	#endregion

	#region Properties

	public override string Description => "Reads content from a file at the specified path.";

	public override string Name => "ReadFile";

	public override string ParametersJsonSchema =>
		"""
		{
			"properties": {
				"path": { 
					"type": "string", 
					"description": "The absolute or relative path to the file to be read." 
				},
				"startByte": { 
					"type": "integer", 
					"description": "The starting byte position. Defaults to 0." 
				},
				"length": { 
					"type": "integer", 
					"description": "The number of bytes to read. Defaults to the end of the file." 
				}
			},
			"required": ["path"],
			"type": "object",
			"additionalProperties": false,
		}
		""";

	#endregion

	#region Methods

	public override async Task<ToolResult> ExecuteAsync(PartialUpdate parameters, CancellationToken ct)
	{
		var filePath = parameters.TryGet<string>("path", out var pathUpdate) ? pathUpdate : "no path provided";
		var startByte = parameters.TryGet<int>("startByte", out var start) ? start : 0;
		var length = parameters.TryGetProperty<int>(out var lengthElem, "length") ? lengthElem : -1;

		if (!ValidatePath(filePath, out var fileInfo))
		{
			return ToolResult.AsError("Error: Path is invalid or outside allowed directory.");
		}

		try
		{
			if (fileInfo.Length > MaxFileSizeBytes)
			{
				return ToolResult.AsError("Error: File exceeds maximum allowed size.");
			}

			if ((startByte < 0) || (startByte >= fileInfo.Length))
			{
				return ToolResult.AsError("Error: Start byte is out of range.");
			}

			await using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
			stream.Seek(startByte, SeekOrigin.Begin);

			var availableBytes = fileInfo.Length - startByte;
			var bytesToRead = length > 0 ? (int) Math.Min(length, availableBytes) : (int) availableBytes;
			if (bytesToRead <= 0)
			{
				return ToolResult.AsSuccess(string.Empty);
			}

			var buffer = new byte[bytesToRead];
			var actualBytesRead = await stream.ReadAsync(buffer, 0, bytesToRead);
			var content = Encoding.UTF8.GetString(buffer, 0, actualBytesRead);

			//OnToolAccessed($"Read file: {fileInfo.FullName} (offset: {startByte}, length: {actualBytesRead})");
			return ToolResult.AsSuccess(content);
		}
		catch (UnauthorizedAccessException)
		{
			// Do not return the absolute path. Exposes absolute paths to the client could allow
			// attackers to use this to map your internal directory structure.
			//return "Error: Access denied to the requested file.";
			return ToolResult.AsError($"Error: Access denied to file '{fileInfo.FullName}'.");
		}
		catch (Exception)
		{
			//OnRawStreamLine($"[Internal] File read error: {ex}");
			return ToolResult.AsError($"Error reading file: Access denied or file not found.\r\n{fileInfo.FullName}");
		}
	}

	#endregion
}