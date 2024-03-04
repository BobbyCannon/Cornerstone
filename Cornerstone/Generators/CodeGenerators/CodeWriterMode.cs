namespace Cornerstone.Generators.CodeGenerators;

/// <summary>
/// The mode for the code writer.
/// </summary>
public enum CodeWriterMode
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