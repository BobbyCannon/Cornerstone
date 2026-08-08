#region References

using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// The priorities at which operations can be invoked via the Dispatcher.
/// </summary>
[SourceReflection]
public enum DispatcherPriority
{
	/// <summary>
	/// Operations at this priority are processed at render priority.
	/// </summary>
	Render,

	/// <summary>
	/// Operations at this priority are processed at normal priority.
	/// </summary>
	Normal,

	/// <summary>
	/// Operations at this priority are processed when the system is idle.
	/// </summary>
	SystemIdle,

	/// <summary>
	/// Operations at this priority are processed when the application is idle.
	/// </summary>
	ApplicationIdle,

	/// <summary>
	/// Operations at this priority are processed when the context is idle.
	/// </summary>
	ContextIdle,

	/// <summary>
	/// Operations at this priority are processed after all other non-idle operations are done.
	/// </summary>
	Background
}