#region References

using System;
using System.Collections.Concurrent;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Text.CodeGenerators;

public static class CodeBuilderPool
{
	#region Constants

	public const int DefaultCapacity = 128;
	public const int MaximumCapacity = 32768;

	#endregion

	#region Fields

	private static readonly ConcurrentBag<CodeBuilder> _pool;

	#endregion

	#region Constructors

	static CodeBuilderPool()
	{
		_pool = [];
	}

	#endregion

	#region Methods

	public static Disposable<CodeBuilder> Rent(uint requestedCapacity = DefaultCapacity)
	{
		if (!_pool.TryTake(out var builder))
		{
			return Disposable<CodeBuilder>.Create(new CodeBuilder(Math.Max(requestedCapacity, DefaultCapacity)), Return);
		}

		//builder.EnsureCapacity(Math.Max(requestedCapacity, DefaultCapacity));
		return Disposable<CodeBuilder>.Create(builder, Return);
	}

	public static void Reset()
	{
		_pool.Clear();
	}

	public static void Return(CodeBuilder builder)
	{
		if (builder == null)
		{
			return;
		}

		builder.Clear();

		// todo: make configurable?
		//if (builder.Capacity > MaximumCapacity)
		//{
		//	builder.Capacity = DefaultCapacity;
		//}

		_pool.Add(builder);
	}

	#endregion
}