#region References

using System.Collections.Concurrent;

#endregion

namespace Cornerstone.Extensions;

public static class ConcurrentExtensions
{
	#region Methods

	public static void Clear<T>(this ConcurrentQueue<T> queue)
	{
		while (!queue.IsEmpty)
		{
			queue.TryDequeue(out _);
		}
	}

	#endregion
}