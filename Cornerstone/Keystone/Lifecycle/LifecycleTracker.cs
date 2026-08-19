#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Keystone.Lifecycle;

public class LifecycleTracker<T> : LifecycleTracker where T : class, ILifecycle
{
	#region Properties

	// Optional but nice: typed enumeration of children
	public IEnumerable<T> TypedChildren => Children.OfType<T>();

	#endregion

	#region Methods

	/// <summary>
	/// Releases a child typed as T.
	/// </summary>
	public T Release(T child)
	{
		return base.Release(child);
	}

	/// <summary>
	/// Releases any lifecycle child and returns it as T2. T2 need not be T
	/// (e.g. a nested LifecycleTracker on a LifecycleTracker of ViewModel).
	/// </summary>
	public new T2 Release<T2>(T2 child) where T2 : class, ILifecycle
	{
		return base.Release(child);
	}

	/// <summary>
	/// Tracks a child typed as T.
	/// </summary>
	public T Track(T child)
	{
		return base.Track(child);
	}

	/// <summary>
	/// Tracks any lifecycle child and returns it as T2. T2 need not be T
	/// (e.g. a nested LifecycleTracker on a LifecycleTracker of ViewModel).
	/// </summary>
	public new T2 Track<T2>(T2 child) where T2 : class, ILifecycle
	{
		return base.Track(child);
	}

	#endregion
}

[SourceReflection]
public class LifecycleTracker : CornerstoneObject, ILifecycleTracker
{
	#region Fields

	protected readonly SpeedyList<ILifecycle> Children;

	#endregion

	#region Constructors

	public LifecycleTracker()
	{
		Children = new SpeedyList<ILifecycle>();
	}

	#endregion

	#region Methods

	public override bool CanProcessLifecycle()
	{
		// Allows processing of children
		// I wonder if this could be made more efficient?
		return true;
	}

	public override void InitializeLifecycle()
	{
		// StartupProfiler.Start is a no-op when the profiler is null.
		using (AppBootstrap.StartupProfiler.Start($"{GetType().Name}.Initialize"))
		{
			base.InitializeLifecycle();
			foreach (var child in Children)
			{
				using (AppBootstrap.StartupProfiler.Start(child.GetType().Name))
				{
					child.InitializeLifecycle();
				}
			}
		}
	}

	public override void LoadLifecycle()
	{
		using (AppBootstrap.StartupProfiler.Start($"{GetType().Name}.Load"))
		{
			foreach (var child in Children)
			{
				using (AppBootstrap.StartupProfiler.Start(child.GetType().Name))
				{
					child.LoadLifecycle();
				}
			}
			base.LoadLifecycle();
		}
	}

	public override void ProcessLifecycle()
	{
		foreach (var child in Children)
		{
			if (child.CanProcessLifecycle())
			{
				child.ProcessLifecycle();
			}
		}

		base.ProcessLifecycle();
	}

	public T Release<T>(T child) where T : ILifecycle
	{
		if (Children.Remove(child))
		{
			OnChildRelease(child);
		}
		return child;
	}

	public override void StartLifecycle()
	{
		using (AppBootstrap.StartupProfiler.Start($"{GetType().Name}.Start"))
		{
			foreach (var child in Children)
			{
				using (AppBootstrap.StartupProfiler.Start(child.GetType().Name))
				{
					child.StartLifecycle();
				}
			}
			base.StartLifecycle();
		}
	}

	public override void StopLifecycle()
	{
		// Reverse order is often safer (children first)
		for (var i = Children.Count - 1; i >= 0; i--)
		{
			Children[i].StopLifecycle();
		}

		base.StopLifecycle();
	}

	public T Track<T>(T child) where T : ILifecycle
	{
		if (child == null)
		{
			throw new ArgumentNullException(nameof(child));
		}
		if (Children.Contains(child))
		{
			return child;
		}

		Children.Add(child);

		OnChildTrack(child);

		return child;
	}

	public override void UninitializeLifecycle()
	{
		// Reverse order is often safer (children first)
		for (var i = Children.Count - 1; i >= 0; i--)
		{
			Children[i].UninitializeLifecycle();
		}

		base.UninitializeLifecycle();
	}

	public override void UnloadLifecycle()
	{
		// Reverse order is often safer (children first)
		for (var i = Children.Count - 1; i >= 0; i--)
		{
			Children[i].UnloadLifecycle();
		}

		base.UnloadLifecycle();
	}

	private static void OnChildRelease(ILifecycle child)
	{
		if (child == null)
		{
			return;
		}
		if (child.IsLifecycleStarted())
		{
			child.StopLifecycle();
		}
		if (child.IsLifecycleLoaded())
		{
			child.UnloadLifecycle();
		}
		if (child.IsLifecycleInitialized())
		{
			child.UninitializeLifecycle();
		}
	}

	private void OnChildTrack(ILifecycle child)
	{
		if (child == null)
		{
			return;
		}

		// Advance the child only as far as the tracker itself have gone
		if (IsLifecycleInitialized()
			&& !child.IsLifecycleInitialized())
		{
			using (AppBootstrap.StartupProfiler.Start(child.GetType().Name))
			{
				child.InitializeLifecycle();
			}
		}

		if (IsLifecycleLoaded()
			&& !child.IsLifecycleLoaded())
		{
			using (AppBootstrap.StartupProfiler.Start(child.GetType().Name))
			{
				child.LoadLifecycle();
			}
		}

		if (IsLifecycleStarted()
			&& !child.IsLifecycleStarted())
		{
			using (AppBootstrap.StartupProfiler.Start(child.GetType().Name))
			{
				child.StartLifecycle();
			}
		}
	}

	#endregion
}

public interface ILifecycleTracker
{
	#region Methods

	public T Release<T>(T child) where T : ILifecycle;
	public T Track<T>(T child) where T : ILifecycle;

	#endregion
}