#region References

using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using System;
using System.IO;
using System.Linq;


#endregion

namespace Cornerstone.Agent.Keystone.State;

/// <summary>
/// Metadata wrapper for a single GGUF model file with optional mmproj support.
/// </summary>
[SourceReflection]
[DependencyInjected]
public partial class ModelInfo : Header.GgufMetadata
{
	#region Fields

	private bool? _isValidGguf;
	private string _mmprojPath;

	#endregion

	#region Constructors

	public ModelInfo(string filePath)
	{
		FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
	}

	#endregion

	#region Properties

	public string FileName => Path.GetFileNameWithoutExtension(FilePath);

	[Notify]
	public partial string FilePath { get; set; }

	public long FileSizeBytes => new FileInfo(FilePath).Length;

	public bool HasMmproj => !string.IsNullOrEmpty(MmprojPath) && File.Exists(MmprojPath!);

	[Notify]
	public partial bool IsActive { get; set; }

	public bool IsValidGguf
	{
		get
		{
			if (_isValidGguf.HasValue)
			{
				return _isValidGguf.Value;
			}

			try
			{
				using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				var header = new byte[4];
				_isValidGguf = (fs.Read(header, 0, 4) == 4) && header.SequenceEqual("GGUF"u8);
			}
			catch
			{
				_isValidGguf = false;
			}

			return _isValidGguf.Value;
		}
	}

	public bool IsVisionModel { get; set; }

	public DateTimeOffset LastModifiedTime => new FileInfo(FilePath).LastWriteTimeUtc;

	/// <summary>
	/// Path to the adjacent .mmproj file. Returns null if not found or FilePath is invalid.
	/// Follows conventions:
	/// - {model_name}.gguf ↔ {model_name}.mmproj
	/// - {model_name}.gguf ↔ mmproj-{model_name}.gguf
	/// </summary>
	public string MmprojPath => GetMmprojPath();

	/// <summary> Size of the mmproj file in bytes. Null if missing. </summary>
	public long? MmprojSizeBytes => HasMmproj ? new FileInfo(MmprojPath!).Length : null;

	#endregion

	#region Methods

	/// <summary>
	/// Loads metadata from a GGUF file
	/// </summary>
	public static ModelInfo Create(string ggufFilePath)
	{
		var metadata = LoadGgufMetadata(ggufFilePath);
		var response = new ModelInfo(ggufFilePath);
		response.UpdateWith(metadata);
		return response;
	}

	private string GetMmprojPath()
	{
		if ((_mmprojPath != null) || string.IsNullOrEmpty(FilePath))
		{
			return _mmprojPath;
		}

		var directory = Path.GetDirectoryName(FilePath) ?? string.Empty;
		var fileNameWithoutExt = Path.GetFileNameWithoutExtension(FilePath);

		// 1. Try Pattern A: The "Extension" style (model.gguf -> model.mmproj)
		var extensionStylePath = Path.Combine(directory, $"{fileNameWithoutExt}.mmproj");
		if (File.Exists(extensionStylePath))
		{
			return _mmprojPath = extensionStylePath;
		}

		// 2. Try Pattern B: The "Naming" style (model.gguf -> mmproj-model.gguf or model-mmproj.gguf)
		// We look for any file in the same directory that contains 'mmproj' and shares the base name
		// Only check gguf files to be safe
		var allFiles = Directory.GetFiles(directory, "*.gguf");

		var bestMatch = allFiles
			.Select(f => new
			{
				Path = f,
				Name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant()
			})
			.Where(x => x.Name.Contains("mmproj")
				&& (x.Name.Contains(fileNameWithoutExt.ToLowerInvariant())
					|| fileNameWithoutExt.ToLowerInvariant().Contains(x.Name))
			)
			.OrderByDescending(x => x.Path.Length)
			.FirstOrDefault();

		if (bestMatch != null)
		{
			return _mmprojPath = bestMatch.Path;
		}

		return _mmprojPath = null;
	}

	#endregion
}