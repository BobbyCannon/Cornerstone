#region References

using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Serialization;
using Cornerstone.Storage.Sql;

#endregion

namespace Cornerstone.Storage.Configuration;

[SourceReflection]
public class PropertyConfiguration
{
	#region Properties

	public PackAttribute Pack { get; set; }

	public string PropertyName { get; set; }

	public SqlTableColumnAttribute SqlTableColumn { get; set; }

	public UpdateableActionAttribute UpdateableAction { get; set; }

	#endregion

	#region Methods

	public SqlTableColumnAttribute AsSqlTableColumn(string columnName)
	{
		SqlTableColumn ??= new SqlTableColumnAttribute();
		SqlTableColumn.Name = columnName;
		return SqlTableColumn;
	}

	public PropertyConfiguration IsPackable(byte version, byte offset)
	{
		Pack ??= new PackAttribute(version, offset);
		return this;
	}

	public UpdateableActionAttribute IsUpdateable(UpdateableAction action)
	{
		UpdateableAction ??= new UpdateableActionAttribute(action);
		UpdateableAction.Actions = action;
		return UpdateableAction;
	}

	#endregion
}