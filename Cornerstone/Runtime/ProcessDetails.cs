#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Results of a process run.
/// </summary>
public class ProcessDetails : Bindable<ProcessDetails>
{
	#region Constructors

	public ProcessDetails() : this(null)
	{
	}

	/// <inheritdoc />
	public ProcessDetails(IDispatcher dispatcher) : base(dispatcher)
	{
		Errors = [];
	}

	#endregion

	#region Properties

	/// <summary>
	/// The arguments used for the run.
	/// </summary>
	public string Arguments { get; set; }

	/// <summary>
	/// How long the process took to run.
	/// </summary>
	[DependsOn(nameof(StartedOn), nameof(StoppedOn))]
	public TimeSpan Duration => StoppedOn - StartedOn;

	/// <summary>
	/// The errors as the process ran.
	/// </summary>
	public List<string> Errors { get; set; }

	/// <summary>
	/// The exception that occurred during the run.
	/// </summary>
	public Exception Exception { get; set; }

	/// <summary>
	/// The code that the process exited with.
	/// </summary>
	public int ExitCode { get; set; }

	/// <summary>
	/// The path to the file of the run.
	/// </summary>
	public string FilePath { get; set; }

	public bool HasErrors => (Exception != null) || Errors is { Count: > 0 };

	/// <summary>
	/// True if the process has started.
	/// </summary>
	public bool IsStarted => StartedOn > DateTime.MinValue;

	/// <summary>
	/// True if the call was on the dispatcher otherwise false.
	/// </summary>
	public bool OnDispatcher { get; set; }

	/// <summary>
	/// The ID of the process during the run.
	/// </summary>
	public int ProcessId { get; set; }

	/// <summary>
	/// The date and time the process started on.
	/// </summary>
	public DateTime StartedOn { get; set; }

	/// <summary>
	/// The date and time the run completed.
	/// </summary>
	public DateTime StoppedOn { get; set; }

	/// <summary>
	/// The process was cancelled and never completed.
	/// </summary>
	public bool WasCancelled { get; set; }

	/// <summary>
	/// The working directory when the process was started.
	/// </summary>
	public string WorkingDirectory { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool UpdateWith(ProcessDetails update, IncludeExcludeSettings settings)
	{
		return this.UpdateWithUsingReflection(update, settings);
	}

	#endregion
}