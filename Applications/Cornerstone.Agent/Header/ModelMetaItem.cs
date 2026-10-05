#region References

using System;
using System.Linq;
using System.Text;

#endregion

namespace Cornerstone.Agent.Header;

public class ModelMetaItem
{
	#region Properties

	public ModelDataTypeEnum? ArrayElementType { get; set; }
	public string[] ArrayStrings { get; set; }
	public ModelDataTypeEnum DataType { get; set; }
	public string Name { get; set; }
	public byte[] RawData { get; set; }

	#endregion

	#region Methods

	public string DataAsString()
	{
		var sb = new StringBuilder();
		return DataAsString(sb);
	}

	public string DataAsString(StringBuilder sb)
	{
		switch (DataType)
		{
			case ModelDataTypeEnum.GgufMetadataValueTypeString:
				sb.Append(Encoding.UTF8.GetString(RawData));
				break;
			case ModelDataTypeEnum.GgufMetadataValueTypeArray:
				if (ArrayElementType == ModelDataTypeEnum.GgufMetadataValueTypeString)
				{
					if (ArrayStrings.Length > 10)
					{
						sb.Append($"{string.Join(", ", ArrayStrings.Take(10))}...");
					}
					else
					{
						sb.Append(string.Join(", ", ArrayStrings));
					}
				}
				else
				{
					sb.Append($"[{Enum.GetName(typeof(ModelDataTypeEnum), ArrayElementType)}]");
				}
				break;
			default:
				sb.Append(Enum.GetName(typeof(ModelDataTypeEnum), DataType));
				break;
		}
		;
		return sb.ToString();
	}

	public override string ToString()
	{
		var sb = new StringBuilder($"{Name}:");
		sb.Append(DataAsString());
		return sb.ToString();
	}

	#endregion
}