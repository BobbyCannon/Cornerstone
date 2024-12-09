#region References

using System;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Data.TypeActivators;

/// <summary>
/// Allows assigning an interface to a specific implementation.
/// </summary>
/// <typeparam name="T"> The interface type. </typeparam>
public class TypeActivator<T> : TypeActivator<T, T>
{
	#region Constructors

	/// <summary>
	/// Create an instance of the type activator.
	/// </summary>
	/// <param name="activator"> The activator for the type. </param>
	public TypeActivator(Func<object[], T> activator) : base(activator)
	{
	}

	#endregion
}

/// <summary>
/// Allows assigning an interface to a specific implementation.
/// </summary>
/// <typeparam name="T"> The interface type. </typeparam>
/// <typeparam name="T2"> The implementation type. </typeparam>
public class TypeActivator<T, T2> : TypeActivator
{
	#region Fields

	private readonly Func<object[], T2> _activator;
	private T2 _currentValue;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public TypeActivator(Func<object[], T2> activator) : base(typeof(T), null)
	{
		_activator = activator;
	}

	#endregion

	#region Methods

	/// <summary>
	/// Create an instance of the Type.
	/// </summary>
	/// <param name="arguments"> The value of the arguments. </param>
	/// <returns> The new instances of the type. </returns>
	public T2 CreateInstance(params object[] arguments)
	{
		Lock.EnterReadLock();

		try
		{
			if ((Lifetime == TypeLifetime.Singleton)
				&& (_currentValue != null))
			{
				return _currentValue;
			}
		}
		finally
		{
			Lock.ExitReadLock();
		}

		Lock.EnterWriteLock();

		try
		{
			if ((Lifetime == TypeLifetime.Singleton)
				&& (_currentValue != null))
			{
				return _currentValue;
			}

			var response = _activator.Invoke(arguments);
			if (Lifetime == TypeLifetime.Singleton)
			{
				_currentValue = response;
			}
			return response;
		}
		finally
		{
			Lock.ExitWriteLock();
		}
	}

	/// <inheritdoc />
	public sealed override object CreateInstanceObject(params object[] arguments)
	{
		return CreateInstance(arguments);
	}

	#endregion
}

/// <summary>
/// Allows assigning an interface to a specific implementation.
/// </summary>
public class TypeActivator
{
	#region Fields

	private readonly Func<object[], object> _activator;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of the type activator.
	/// </summary>
	/// <param name="type"> The type this activator is for. </param>
	/// <param name="activator"> The activator to create the type. </param>
	public TypeActivator(Type type, Func<object[], object> activator)
	{
		_activator = activator;
		Lock = new ReaderWriterLockTiny();
		Type = type;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The type this activator is for.
	/// </summary>
	public Type Type { get; }

	/// <summary>
	/// The lock for handling singleton creation.
	/// </summary>
	protected ReaderWriterLockTiny Lock { get; }

	/// <summary>
	/// True if this activator is for dependency injection.
	/// </summary>
	internal bool ForDependencyInjection { get; set; }

	/// <summary>
	/// This is the lifetime of the value. It is how long the value will live.
	/// </summary>
	internal TypeLifetime Lifetime { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Create an instance of the Type.
	/// </summary>
	/// <param name="arguments"> The value of the arguments. </param>
	/// <returns> The new instances of the type. </returns>
	public virtual object CreateInstanceObject(params object[] arguments)
	{
		return _activator.Invoke(arguments);
	}

	#endregion
}