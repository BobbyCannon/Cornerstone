#region References

using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Generators;

/// <summary>
/// Interface for a code string provider.
/// </summary>
public interface ICodeStringProvider
{
	#region Methods

	/// <summary>
	/// Convert the object to a code text.
	/// </summary>
	/// <param name="asNullable"> An optional as nullable flag. True should cast as nullable version. </param>
	/// <param name="builder"> An optional builder to continue to build with. </param>
	/// <param name="language"> Optional format for generating code. Defaults to CSharp. </param>
	/// <param name="settings"> Optional settings to use when creating the code string. </param>
	/// <returns> The object as a code text format. </returns>
	TextBuilder ToCodeString(bool asNullable = false, TextBuilder builder = null, CodeLanguage language = CodeLanguage.CSharp, CodeWriterOptions? settings = null);

	#endregion
}