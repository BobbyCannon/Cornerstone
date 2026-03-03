namespace Cornerstone.Generators.Models;

public class SourceTypeParameterInfo
{
	#region Properties

	public string[] ConstraintTypes { get; init; }
	public bool HasTypeParameterInConstraintTypes { get; init; }
	public bool HasUnmanagedTypeConstraint { get; init; }
	public bool HasValueTypeConstraint { get; init; }
	public string Name { get; init; }

	#endregion
}