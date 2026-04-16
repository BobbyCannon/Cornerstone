#region References

using Cornerstone.Generators.Models;

#endregion

namespace Cornerstone.Generators.Processors;

/// <summary>
/// Defines a per-type source generation processor.
/// </summary>
internal interface ITypeProcessor
{
	#region Properties

	/// <summary>
	/// When true, the processor emits code inside the partial type declaration.
	/// When false, the processor emits code outside the type block (e.g., into a helper class).
	/// </summary>
	bool EmitsInsideTypeBlock { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Process the type and emit generated code into the builder.
	/// Implementations should return early when there is nothing to generate.
	/// </summary>
	void Process(CSharpCodeBuilder builder, SourceTypeInfo type);

	#endregion
}