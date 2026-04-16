namespace Cornerstone.Generators.Models;

public class UpdateablePropertyOrder
{
	#region Properties

	public int Action { get; set; }
	
	public bool CanWrite { get; set; }

	public bool CanAccess { get; set; }

	public string Name { get; set; }

	public int Order { get; set; }

	#endregion
}