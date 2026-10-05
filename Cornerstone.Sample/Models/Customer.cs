#region References

using Cornerstone.Data;
using Cornerstone.Reflection;
using Cornerstone.Serialization;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Models;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
[Packable(1, ["*"])]
public partial class Customer
	: SyncModel, ICustomer,
		IUpdateable<Customer>,
		IUpdateable<CustomerEntity>,
		IUpdateable<ICustomer>
{
	#region Properties

	public partial string Name { get; set; }

	#endregion
}

public interface ICustomer
{
	#region Properties

	public string Name { get; set; }

	#endregion
}
