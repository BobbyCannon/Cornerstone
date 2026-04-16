#region References

using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Storage.Sql;

#endregion

namespace Cornerstone.Storage.Configuration;

[SourceReflection]
public class TypeConfiguration
{
	#region Constructors

	public TypeConfiguration()
	{
		Properties = [];
	}

	#endregion

	#region Properties

	public Dictionary<string, PropertyConfiguration> Properties { get; }

	public SqlTableAttribute SqlTable { get; set; }

	#endregion

	#region Methods

	public SqlTableAttribute AsTable(string tableName)
	{
		SqlTable ??= new SqlTableAttribute();
		SqlTable.TableName = tableName;
		return SqlTable;
	}

	public void Packable(byte version, params string[] properties)
	{
		for (byte offset = 1; offset <= properties.Length; offset++)
		{
			var property = properties[offset];
			Property(property).IsPackable(version, offset);
		}
	}

	public PropertyConfiguration Property(string propertyName)
	{
		if (Properties.TryGetValue(propertyName, out var response))
		{
			return response;
		}
		response = new PropertyConfiguration { PropertyName = propertyName };
		Properties[propertyName] = response;
		return response;
	}

	public void Updateable(UpdateableAction action, params string[] properties)
	{
		for (byte offset = 1; offset <= properties.Length; offset++)
		{
			var property = properties[offset];
			Property(property).IsUpdateable(action);
		}
	}

	#endregion
}