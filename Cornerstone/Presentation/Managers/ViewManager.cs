#region References

using System;
using Cornerstone.Attributes;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Presentation.Managers;

/// <summary>
/// Represents a manager of a set of views.
/// </summary>
public abstract class ViewManager<T> : SpeedyList<T>, IManager
{
	#region Constructors

	/// <summary>
	/// Initialize the view manager.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected ViewManager(IDispatcher dispatcher) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	[SerializationIgnore]
	public bool IsInitialized { get; private set; }

	/// <inheritdoc />
	[SerializationIgnore]
	public bool IsLoaded { get; private set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public virtual void Initialize()
	{
		IsInitialized = true;
	}

	/// <inheritdoc />
	public virtual void Load(params object[] values)
	{
		IsLoaded = true;
	}

	/// <inheritdoc />
	public virtual void Process()
	{
	}

	/// <inheritdoc />
	public virtual void Uninitialize()
	{
		Unload();
		IsInitialized = false;
	}

	/// <inheritdoc />
	public virtual void Unload()
	{
		IsLoaded = false;
	}

	#endregion
}