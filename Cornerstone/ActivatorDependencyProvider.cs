#region References

using System;

#endregion

namespace Cornerstone;

public class ActivatorAsDependencyProvider : IDependencyProvider
{
	#region Methods

	/// <inheritdoc />
	public T GetInstance<T>()
	{
		return Activator.CreateInstance<T>();
	}

	/// <inheritdoc />
	public object GetInstance(Type type)
	{
		return type.CreateInstance();
	}

	#endregion
}