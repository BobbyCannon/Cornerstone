#region References

using System;
using Serilog.Events;

#endregion

namespace Cornerstone.VisualStudio;

/// <summary>
/// Design-time / XAML-friendly list of Serilog levels (markup cannot resolve
/// <c>x:Type serilog:LogEventLevel</c> reliably with package references).
/// </summary>
public static class LogEventLevels
{
	#region Properties

	public static Array All { get; } = Enum.GetValues(typeof(LogEventLevel));

	#endregion
}