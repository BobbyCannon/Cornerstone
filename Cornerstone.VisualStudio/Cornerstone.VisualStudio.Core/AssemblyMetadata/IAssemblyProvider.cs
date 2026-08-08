#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.VisualStudio.Core.AssemblyMetadata;

public interface IAssemblyProvider
{
	#region Methods

	IEnumerable<string> GetAssemblies();

	#endregion
}