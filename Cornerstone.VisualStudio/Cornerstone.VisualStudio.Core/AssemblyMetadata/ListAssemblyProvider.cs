#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.VisualStudio.Core.AssemblyMetadata;

public sealed class ListAssemblyProvider : IAssemblyProvider
{
	#region Fields

	private readonly IEnumerable<string> _paths;

	#endregion

	#region Constructors

	public ListAssemblyProvider(IEnumerable<string> paths)
	{
		_paths = paths ?? new List<string>();
	}

	#endregion

	#region Methods

	public IEnumerable<string> GetAssemblies()
	{
		return _paths;
	}

	#endregion
}
