#region References

using System;

#endregion

namespace Cornerstone.Data.TypeActivators;

/// <summary>
/// Create a type with a custom provider.
/// </summary>
/// <typeparam name="T"> The type to be created. </typeparam>
public class GenericTypeActivator<T> : TypeActivator<T>
{
	#region Fields

	private readonly Func<object[], T> _create;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public GenericTypeActivator(Func<object[], T> create)
	{
		_create = create;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override object CreateInstance(params object[] arguments)
	{
		return _create(arguments);
	}

	#endregion
}