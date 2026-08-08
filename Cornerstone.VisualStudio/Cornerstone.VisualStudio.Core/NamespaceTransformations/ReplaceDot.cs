#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.VisualStudio.Core.NamespaceTransformations;

internal class ReplaceDot : INamespaceTransformation
{
	#region Fields

	private readonly char _substitution;

	#endregion

	#region Constructors

	public ReplaceDot(char substitution)
	{
		_substitution = substitution;
	}

	#endregion

	#region Methods

	public IEnumerable<char> Apply(IEnumerable<char> input)
	{
		foreach (var c in input)
		{
			yield return c == '.' ? _substitution : c;
		}
	}

	#endregion
}