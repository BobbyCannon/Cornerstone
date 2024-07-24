#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Internal;

/// <summary>
/// The source of a location provider.
/// </summary>
internal class SourceInformationProvider : Bindable, IInformationProvider
{
	#region Constructors

	/// <summary>
	/// Instantiate an instance of a location provider source.
	/// </summary>
	public SourceInformationProvider() : this(null)
	{
	}

	/// <summary>
	/// Instantiate an instance of a location provider source.
	/// </summary>
	public SourceInformationProvider(IDispatcher dispatcher) : base(dispatcher)
	{
		SubProviders = Array.Empty<IInformationProvider>();

		// Commands
		StartMonitoringCommand = new RelayCommand(_ => StartMonitoringAsync());
		StopMonitoringCommand = new RelayCommand(_ => StopMonitoringAsync());
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public bool HasSubProviders => SubProviders.Any();

	/// <inheritdoc />
	public bool IsEnabled { get; set; }

	/// <inheritdoc />
	public bool IsMonitoring { get; set; }

	/// <inheritdoc />
	public string ProviderName { get; set; }

	/// <inheritdoc />
	public ICommand StartMonitoringCommand { get; }

	/// <inheritdoc />
	public ICommand StopMonitoringCommand { get; }

	/// <inheritdoc />
	public IEnumerable<IInformationProvider> SubProviders { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public Task StartMonitoringAsync()
	{
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task StopMonitoringAsync()
	{
		return Task.CompletedTask;
	}

	protected virtual void OnUpdated(IUpdateable e)
	{
		Updated?.Invoke(this, e);
	}

	#endregion

	#region Events

	public event EventHandler<IUpdateable> Updated;

	#endregion
}