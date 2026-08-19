#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using LLama;

#endregion

namespace Cornerstone.Agent.Keystone;

/// <summary>
/// Native LLama weights shared by models and agent processors.
/// Processors must not reference each other; both use this runtime.
/// </summary>
[SourceReflection]
[DependencyInjected]
public class ModelWeightsRuntime
{
	#region Fields

	private Func<string, CancellationToken, Task<bool>> _ensureLoadedAsync;

	#endregion

	#region Properties

	public MtmdWeights LlavaWeights { get; set; }

	public LLamaWeights Weights { get; set; }

	#endregion

	#region Methods

	public Task<bool> EnsureLoadedAsync(string desiredPath, CancellationToken cancellationToken)
	{
		if (_ensureLoadedAsync == null)
		{
			return Task.FromResult(false);
		}

		return _ensureLoadedAsync(desiredPath, cancellationToken);
	}

	public void NotifyUnloading()
	{
		Unloading?.Invoke();
	}

	public void SetEnsureLoaded(Func<string, CancellationToken, Task<bool>> ensureLoadedAsync)
	{
		_ensureLoadedAsync = ensureLoadedAsync;
	}

	#endregion

	#region Events

	public event Action Unloading;

	#endregion
}
