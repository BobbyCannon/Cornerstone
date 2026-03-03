namespace Cornerstone.Text.CodeGenerators;

/// <summary>
/// The output for the code builder.
/// </summary>
public enum CodeBuilderOutput
{
	/// <summary>
	/// As an instance code string.
	/// Ex. new Person { Name = "John Doe" };
	/// </summary>
	Instance = 0,

	/// <summary>
	/// As a declaration code string.
	/// Ex.
	/// new class Person
	/// {
	/// public string Name = "John Doe";
	/// };
	/// </summary>
	Declaration = 1
}