#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.VisualStudio.Core.AssemblyMetadata;

public interface IMetadataProvider
{
	#region Methods

	IMetadataReaderSession GetMetadata(IEnumerable<string> paths);

	#endregion
}

public interface IMetadataReaderSession : IDisposable
{
	#region Properties

	IReadOnlyCollection<IAssemblyInformation> Assemblies { get; }
	string TargetAssemblyName { get; }

	#endregion
}