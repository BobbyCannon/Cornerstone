using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Platform.Storage;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Controls.Platform;

/// <summary>
/// Factory allows to register custom storage provider instead of native implementation.
/// </summary>
[Unstable]
public interface IStorageProviderFactory
{
    IStorageProvider CreateProvider(TopLevel topLevel);
}
