#region References

using System;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Presentation.Managers;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a view manager.
/// </summary>
public abstract class ManagerCollection : Manager
{
	#region Constructors

	/// <summary>
	/// Initialize a view manager.
	/// </summary>
	protected ManagerCollection() : this(null)
	{
	}

	/// <summary>
	/// Initializes a viewmodel.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	protected ManagerCollection(IDispatcher dispatcher) : base(dispatcher)
	{
		Managers = new SpeedyList<IManager>(dispatcher);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The list of managers for this view model.
	/// </summary>
	public SpeedyList<IManager> Managers { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Initialize()
	{
		Managers.ListUpdated += ManagersOnListUpdated;

		foreach (var manager in Managers)
		{
			manager.Initialize();
		}

		base.Initialize();
	}

	/// <inheritdoc />
	public override void Load(params object[] values)
	{
		foreach (var manager in Managers)
		{
			manager.Load();
		}

		base.Load(values);
	}

	/// <inheritdoc />
	public override void Uninitialize()
	{
		Unload();
		Managers.ListUpdated -= ManagersOnListUpdated;
		base.Uninitialize();
	}

	/// <inheritdoc />
	public override void Unload()
	{
		Managers.Clear();
		base.Unload();
	}

	/// <summary>
	/// Add a manager to track.
	/// </summary>
	/// <typeparam name="T"> The type of the manager. </typeparam>
	/// <param name="manager"> The manager to add. </param>
	/// <returns> The manager. </returns>
	protected T AddManager<T>(T manager) where T : IManager
	{
		if (!Managers.Contains(manager))
		{
			Managers.Add(manager);
		}
		return manager;
	}

	private void ManagersOnListUpdated(object sender, SpeedyListUpdatedEventArg e)
	{
		e.Removed?
			.Cast<IManager>()
			.ForEach(manager =>
			{
				manager.Uninitialize();

				if (manager is IDisposable disposable)
				{
					disposable.Dispose();
				}
			});

		e.Added?
			.Cast<IManager>()
			.ForEach(manager => { manager.Initialize(); });
	}

	#endregion
}