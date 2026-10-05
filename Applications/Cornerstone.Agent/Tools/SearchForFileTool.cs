#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Agent.Tools;

public class SearchForFileTool : AgentTool
{
	#region Constructors

	public SearchForFileTool(Keystone.State.AppSettings appSettings) : base(appSettings)
	{
	}

	#endregion

	#region Properties

	public override string Description => "Searches for files by name or pattern across directories. Much faster than SearchFiles when you only need the file path.";

	public override string Name => "SearchForFile";

	public override string ParametersJsonSchema =>
		"""
		{
			"type": "object",
			"properties": {
				"fileName": { 
					"type": "string", 
					"description": "The file name or pattern to search for (e.g., '*.cs', 'Serializer.cs')." 
				},
				"rootDirectory": { 
					"type": "string", 
					"description": "The directory to search within. Defaults to the allowed file directories." 
				},
			},
			"required": ["fileName"]
		}
		""";

	#endregion

	#region Methods

	public override async Task<ToolResult> ExecuteAsync(PartialUpdate parameters, CancellationToken ct)
	{
		var fileName = "no file name provided";
		if (parameters.TryGet<string>("fileName", out var fileNameElement))
		{
			fileName = fileNameElement ?? "no file name provided";
		}

		var rootDirectory = string.Empty;
		if (parameters.TryGet<string>("rootDirectory", out var rootDirectoryElement))
		{
			rootDirectory = rootDirectoryElement ?? string.Empty;
		}

		var extension = "no extension provided";
		if (parameters.TryGet<string>("extension", out var extensionElement))
		{
			extension = extensionElement ?? "no extension provided";
		}

		if (string.IsNullOrWhiteSpace(fileName))
		{
			return ToolResult.AsError("Error: 'fileName' parameter is required.");
		}

		var searchRoots = new List<string>();

		if (!string.IsNullOrWhiteSpace(rootDirectory))
		{
			if (ValidatePath(rootDirectory, out var searchRootInfo))
			{
				var dirPath = searchRootInfo.Directory?.FullName ?? searchRootInfo.FullName;
				searchRoots.Add(dirPath);
			}
			else
			{
				return ToolResult.AsError("Error: Search root is invalid or outside allowed directory.");
			}
		}
		else
		{
			foreach (var allowedDir in Settings.AllowedDirectories)
			{
				if (string.IsNullOrWhiteSpace(allowedDir))
				{
					continue;
				}
				var dirInfo = new DirectoryInfo(allowedDir);
				if (dirInfo.Exists)
				{
					searchRoots.Add(dirInfo.FullName);
				}
			}

			if (searchRoots.Count == 0)
			{
				return ToolResult.AsError("Error: No valid allowed directories found.");
			}
		}

		var searchPattern = fileName;
		if (string.IsNullOrEmpty(extension) || (extension == "no extension provided"))
		{
			if (!searchPattern.Contains("*"))
			{
				searchPattern = $"*{searchPattern}*";
			}
		}
		else
		{
			if (!searchPattern.Contains("*"))
			{
				searchPattern = searchPattern.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? $"*{searchPattern}" : $"*{searchPattern}{extension}";
			}
		}

		var results = new ConcurrentBag<string>();
		var scannedCount = 0;
		var searchLock = new object();
		var maxFilesToScan = Math.Max(MaxFilesToScan, 50000);

		foreach (var root in searchRoots)
		{
			try
			{
				var dirs = new Queue<string>();
				dirs.Enqueue(root);
				var depth = 0;

				while ((dirs.Count > 0) && (depth < MaxSearchDepth))
				{
					var currentBatch = dirs.ToArray();
					dirs.Clear();

					foreach (var dir in currentBatch)
					{
						try
						{
							if (!Directory.Exists(dir))
							{
								continue;
							}

							await Parallel.ForEachAsync(
								Directory.EnumerateFileSystemEntries(dir, searchPattern, SearchOption.TopDirectoryOnly),
								new ParallelOptions { MaxDegreeOfParallelism = 1 },
								(entry, _) =>
								{
									try
									{
										lock (searchLock)
										{
											if (++scannedCount > maxFilesToScan)
											{
												return ValueTask.CompletedTask;
											}
										}

										var attributes = File.GetAttributes(entry);
										if ((attributes & FileAttributes.Directory) != FileAttributes.Directory)
										{
											results.Add(entry);
										}
										return ValueTask.CompletedTask;
									}
									catch (Exception exception)
									{
										return ValueTask.FromException(exception);
									}
								});

							foreach (var subDir in Directory.EnumerateDirectories(dir))
							{
								dirs.Enqueue(subDir);
							}
						}
						catch (UnauthorizedAccessException)
						{
						}
						catch (IOException)
						{
						}
					}
					depth++;
				}
			}
			catch (Exception)
			{
				//OnRawStreamLine($"[Internal] File search failed in {root}: {ex}");
			}
		}

		return ToolResult.AsSuccess(string.Join("\n", results));
	}

	#endregion
}