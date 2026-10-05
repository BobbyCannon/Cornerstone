namespace Cornerstone.Agent.Header;

public class ModelTensorInfo
{
	#region Properties

	public uint DimensionCount { get; set; }
	public ulong[] Dimensions { get; set; }
	public string Name { get; set; }
	public ulong Offset { get; set; }
	public ulong Size { get; set; }
	public ModelTensorType TensorType { get; set; }

	#endregion
}