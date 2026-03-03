namespace Cornerstone.Generators.Models;

public class UpdateablePropertyOrder
{
	#region Properties

	public int Action { get; set; }

	public bool IsReadOnly { get; set; }

	public string Name { get; set; }

	public int Order { get; set; }

	#endregion
}