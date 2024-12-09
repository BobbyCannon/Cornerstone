#region References

using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.FodyTools;

/// <summary>
/// A generic logger interface to decouple implementation.
/// </summary>
internal interface ILogger
{
	#region Methods

	/// <summary>
	/// Logs a debug message.
	/// </summary>
	/// <param name="message"> The message. </param>
	void LogDebug(string message);

	/// <summary>
	/// Logs an error.
	/// </summary>
	/// <param name="message"> The message. </param>
	/// <param name="sequencePoint"> The optional sequence point where the error occurred. </param>
	void LogError(string message, SequencePoint sequencePoint = null);

	/// <summary>
	/// Logs an error.
	/// </summary>
	/// <param name="message"> The message. </param>
	/// <param name="method"> The method where the error occurred. </param>
	void LogError(string message, MethodReference method);

	/// <summary>
	/// Logs an info message.
	/// </summary>
	/// <param name="message"> The message. </param>
	void LogInfo(string message);

	/// <summary>
	/// Logs a warning.
	/// </summary>
	/// <param name="message"> The message. </param>
	/// <param name="sequencePoint"> The optional sequence point where the problem occurred. </param>
	void LogWarning(string message, SequencePoint sequencePoint = null);

	/// <summary>
	/// Logs a warning.
	/// </summary>
	/// <param name="message"> The message. </param>
	/// <param name="method"> The method where the problem occurred. </param>
	void LogWarning(string message, MethodReference method);

	#endregion
}