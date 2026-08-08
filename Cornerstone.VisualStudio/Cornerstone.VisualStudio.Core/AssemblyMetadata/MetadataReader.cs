namespace Cornerstone.VisualStudio.Core.AssemblyMetadata;

public class MetadataReader
{
	#region Fields

	private readonly IMetadataProvider _provider;

	#endregion

	#region Constructors

	public MetadataReader(IMetadataProvider provider)
	{
		_provider = provider;
	}

	#endregion

	#region Methods

	public Metadata GetForTargetAssembly(IAssemblyProvider assemblyProvider)
	{
		using var session = _provider.GetMetadata(assemblyProvider.GetAssemblies());
		return MetadataConverter.ConvertMetadata(session);
	}

	#endregion
}