namespace Cornerstone.Weaver.TestAssembly;

public interface IAccount
{
	#region Properties

	public int Age { get; set; }

	public string FirstName { get; set; }

	public string FullName { get; }

	public string LastName { get; set; }

	#endregion
}