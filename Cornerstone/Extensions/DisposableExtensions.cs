#region References

using System;
using System.Runtime.CompilerServices;

#endregion

namespace Cornerstone.Extensions;

public static class DisposableExtensions
{
	#region Methods

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void TryDispose(object value)
	{
		if (value is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	#endregion
}