#region References

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;

#endregion

namespace Cornerstone.VisualStudio.Extensibility;

/// <summary>
/// In-proc VisualStudio.Extensibility entrypoint for modern Settings contributions.
/// </summary>
/// <remarks>
/// Coexists with <see cref="CornerstonePackage"/> (designer, MEF, editor factory).
/// Settings definitions live in <see cref="CornerstoneSettingDefinitions"/>;
/// runtime sync is performed by <see cref="CornerstoneSettingsBridge"/> from the package.
/// </remarks>
[VisualStudioContribution]
internal sealed class CornerstoneExtension : Extension
{
	#region Properties

	/// <inheritdoc />
	public override ExtensionConfiguration ExtensionConfiguration => new()
	{
		RequiresInProcessHosting = true
	};

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void InitializeServices(IServiceCollection serviceCollection)
	{
		base.InitializeServices(serviceCollection);
	}

	#endregion
}