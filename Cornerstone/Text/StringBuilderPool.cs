#region References

using System;
using System.Collections.Concurrent;
using System.Text;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Text;

public static class StringBuilderPool
{
	#region Constants

	public const int DefaultCapacity = 128;
	public const int MaximumCapacity = 32768;

	#endregion

	#region Fields

	private static readonly ConcurrentBag<StringBuilder> _pool;

	#endregion

	#region Constructors

	static StringBuilderPool()
	{
		_pool = [];
	}

	#endregion

	#region Methods

	public static Disposable<StringBuilder> Rent(int requestedCapacity = DefaultCapacity)
	{
		if (!_pool.TryTake(out var sb))
		{
			return Disposable<StringBuilder>.Create(new StringBuilder(Math.Max(requestedCapacity, DefaultCapacity)), Return);
		}

		sb.EnsureCapacity(Math.Max(requestedCapacity, DefaultCapacity));
		return Disposable<StringBuilder>.Create(sb, Return);
	}

	public static void Reset()
	{
		_pool.Clear();
	}

	public static void Return(StringBuilder sb)
	{
		if (sb == null)
		{
			return;
		}

		sb.Clear();

		// todo: make configurable?
		if (sb.Capacity > MaximumCapacity)
		{
			sb.Capacity = DefaultCapacity;
		}

		_pool.Add(sb);
	}

	#endregion
}