#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.VisualStudio.Core.NamespaceTransformations;

internal class ToLowerTransformation : INamespaceTransformation
{
	#region Methods

	public IEnumerable<char> Apply(IEnumerable<char> input)
	{
		foreach (var c in input)
		{
			yield return char.ToLowerInvariant(c);
		}
	}

	#endregion
}