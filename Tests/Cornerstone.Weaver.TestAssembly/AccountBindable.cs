#region References

using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Weaver.TestAssembly;

public class AccountBindable : Bindable, IAccount
{
	#region Properties

	public int Age { get; set; }

	public string FirstName { get; set; }

	public string FullName => $"{FirstName} {LastName}";

	public string LastName { get; set; }

	#endregion
}