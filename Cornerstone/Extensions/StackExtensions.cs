#region References
#if NETSTANDARD
using System.Collections.Generic;
#endif
#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for Stack
/// </summary>
public static class StackExtensions
{
	#region Methods

	#if NETSTANDARD
	/// <summary>
	/// Returns a value that indicates whether there is an object at the top of the Stack,
	/// and if one is present, copies it to the result parameter, and removes it from the Stack.
	/// </summary>
	/// <typeparam name="T"> The type in the stack. </typeparam>
	/// <param name="stack"> The stack to process. </param>
	/// <param name="value"> The value if popped. </param>
	/// <returns> True if a value is popped otherwise false. </returns>
	public static bool TryPop<T>(this Stack<T> stack, out T value)
	{
		try
		{
			value = stack.Pop();
			return true;
		}
		catch
		{
			value = default;
			return false;
		}
	}

	#endif

	#endregion
}