#region References

using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Agent.Header;

[SourceReflection]
[Updateable(UpdateableAction.All, ["*"])]
public partial class GgufMetadata : CornerstoneObject, IUpdateable<GgufMetadata>
{
	#region Properties

	public string Architecture { get; set; }
	public bool HasVision { get; set; }
	public string ModelName { get; set; }
	public string Quantization { get; set; }
	public ulong TensorCount { get; set; }
	public uint Version { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Loads metadata from a GGUF file
	/// </summary>
	public static GgufMetadata LoadGgufMetadata(string ggufFilePath)
	{
		var response = new GgufMetadata();
		var reader = new ModelHeaderReader();
		var metadata = reader.Read(ggufFilePath);
		response.Architecture = metadata.GetValueOrDefault("general.architecture") ?? "Unknown";
		response.ModelName = metadata.GetValueOrDefault("general.name")
			?? metadata.GetValueOrDefault("general.basename")
			?? string.Empty;

		response.Quantization = metadata.GetValueOrDefault("general.quantization_version")
			?? metadata.GetValueOrDefault("quantization.version")
			?? "Unknown";

		response.HasVision = metadata.ContainsKey("mmproj")
			|| metadata.ContainsKey("general.vision")
			|| metadata.ContainsKey("vision");

		response.Version = uint.TryParse(metadata.GetValueOrDefault("general.file_version"), out var v) ? v : 0;
		response.TensorCount = ulong.TryParse(metadata.GetValueOrDefault("general.tensor_count"), out var t) ? t : 0;

		return response;
	}

	#endregion
}