using System.Collections.Generic;
using System.Linq;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime;

/// <summary>
/// Wraps a <see cref="ICornerstoneXamlIlParentStackProvider"/> into a <see cref="ICornerstoneXamlIlEagerParentStackProvider"/>,
/// for backwards compatibility.
/// </summary>
internal sealed class XamlIlParentStackProviderWrapper : ICornerstoneXamlIlEagerParentStackProvider
{
    private readonly ICornerstoneXamlIlParentStackProvider _provider;

    private IReadOnlyList<object>? _directParentsStack;

    public XamlIlParentStackProviderWrapper(ICornerstoneXamlIlParentStackProvider provider)
        => _provider = provider;

    public IEnumerable<object> Parents
        => _provider.Parents;

    public IReadOnlyList<object> DirectParentsStack
        => _directParentsStack ??= _provider.Parents.Reverse().ToArray();

    public ICornerstoneXamlIlEagerParentStackProvider? ParentProvider
        => null;
}
