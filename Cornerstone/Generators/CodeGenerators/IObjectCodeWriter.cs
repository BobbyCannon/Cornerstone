namespace Cornerstone.Generators.CodeGenerators;

/// <summary>
/// Used to allow objects to control code conversion.
/// </summary>
public interface IObjectCodeWriter
{
	#region Methods

	/// <summary>
	/// Write the object to the provided writer.
	/// </summary>
	/// <param name="writer"> The code writer to write the code to. </param>
	void Write(ICodeWriter writer);

	#endregion
}