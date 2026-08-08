#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.VisualStudio.Core;

public interface INamespaceTransformation
{
	#region Methods

	public IEnumerable<char> Apply(IEnumerable<char> input);

	#endregion
}