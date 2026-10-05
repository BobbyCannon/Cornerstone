#region References

using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Agent.Header;

public class ModelFile
{
	#region Properties

	public ulong DataStartOffset { get; set; }
	public string FilePath { get; set; }
	public List<ModelMetaItem> MetaItems { get; set; }
	public List<ModelTensorInfo> TensorInfos { get; set; }
	public uint Version { get; set; }

	#endregion

	#region Methods

	public bool ContainsKey(string key)
	{
		return MetaItems.Any(x => x.Name == key);
	}

	public string GetValueOrDefault(string key)
	{
		return MetaItems.FirstOrDefault(x => x.Name == key)?.DataAsString();
	}

	#endregion
}