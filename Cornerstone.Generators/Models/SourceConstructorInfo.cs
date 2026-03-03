#region References

using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceConstructorInfo : SourceMemberInfo
{
	#region Fields

	public Accessibility Accessibility;
	public INamedTypeSymbol ContainingType;
	public bool IsDependencyConstructor;
	public bool IsStatic;
	public SourceParameterInfo[] Parameters;

	#endregion
}