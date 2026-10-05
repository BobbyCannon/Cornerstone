#region References

using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Models;

[SourceReflection]
[Notifiable(["*"])]
[SqlTable(TableName = "Customers")]
[Updateable(UpdateableAction.All, [nameof(Name)])]
public partial class CustomerEntity
	: SyncEntity<int>, ICustomer,
		IUpdateable<CustomerEntity>,
		IUpdateable<Customer>,
		IUpdateable<ICustomer>
{
	#region Properties

	[SqlTableColumn(IsNullable = false)]
	public partial string Name { get; set; }

	#endregion
}
