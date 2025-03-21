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
	where T2 : T
{
	#region Fields

	private readonly Func<object[], T2> _activator;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public TypeActivator(Func<object[], T2> activator) : base(typeof(T), null)
	{
		_activator = activator;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected sealed override object CreateInstanceObject(params object[] arguments)
	{
		var instance = _activator.Invoke(arguments);
		T response = instance;
		return response;
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
	private object _currentValue;
	private readonly ReaderWriterLockTiny _lock;

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
		_lock = new ReaderWriterLockTiny();

		Type = type;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The type this activator is for.
	/// </summary>
	public Type Type { get; }

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
	public object GetOrCreateInstance(params object[] arguments)
	{
		try
		{
			_lock.EnterUpgradeableReadLock();

			if ((Lifetime == TypeLifetime.Singleton)
				&& (_currentValue != null))
			{
				return _currentValue;
			}

			_lock.EnterWriteLock();

			try
			{
				if ((Lifetime == TypeLifetime.Singleton)
					&& (_currentValue != null))
				{
					return _currentValue;
				}

				var response = CreateInstanceObject(arguments);
				if (Lifetime == TypeLifetime.Singleton)
				{
					_currentValue = response;
				}

				return response;
			}
			finally
			{
				_lock.ExitWriteLock();
			}
		}
		finally
		{
			_lock.ExitUpgradeableReadLock();
		}
	}

	/// <summary>
	/// Create an instance of the Type.
	/// </summary>
	/// <param name="arguments"> The value of the arguments. </param>
	/// <returns> The new instances of the type. </returns>
	protected virtual object CreateInstanceObject(params object[] arguments)
	{
		return _activator.Invoke(arguments);
	}

	#endregion
}