namespace Cornerstone.Keystone.Lifecycle;

/// <summary>
/// The lifecycle order is:
/// Initialize, Load, Start, [App Run], Stop, Unload, Uninitialize
/// </summary>
public interface ILifecycle : IInitializableLifecycle, ILoadableLifecycle, IProcessableLifecycle, IStartableLifecycle
{
}