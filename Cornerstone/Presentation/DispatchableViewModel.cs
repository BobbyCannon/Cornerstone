#region References

using System;
using System.Collections.Generic;
using System.Threading;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Presentation;

public class DispatchableViewModel<T> : DispatchableViewModel
	where T : IUpdateable, ITrackPropertyChanges
{
	#region Constructors

	public DispatchableViewModel(T model)
	{
		Model = model;
	}

	#endregion

	#region Properties

	public T Model { get; }

	protected bool AutoUpdateModel { get; set; }

	#endregion

	#region Methods

	public override void ApplyModelChanges()
	{
		base.ApplyModelChanges();

		if (Model.HasChanges())
		{
			Model.ApplyChangesTo(this);
			Model.ResetHasChanges();
		}
	}

	public override bool HasModelChanges()
	{
		return base.HasModelChanges() || Model.HasChanges();
	}

	public override void LoadLifecycle()
	{
		UpdateWith(Model);
		ResetHasChanges();
		base.LoadLifecycle();
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		base.OnPropertyChanged(propertyName, oldValue, newValue);

		if (IsLifecycleLoaded() && AutoUpdateModel)
		{
			Model.UpdatePropertyWith(propertyName, newValue);
			base.ResetHasChanged(propertyName);
		}
	}

	#endregion
}

[SourceReflection]
public abstract partial class DispatchableViewModel : ViewModel
{
	#region Fields

	private readonly HashSet<object> _attachOwners;
	private readonly object _attachSync;
	private List<IDispatchBinding> _bindings;
	private List<DispatchableViewModel> _dispatchChildren;
	private int _isAttachedFlag;

	#endregion

	#region Constructors

	protected DispatchableViewModel()
	{
		_attachOwners = [];
		_attachSync = new();
	}

	#endregion

	#region Properties

	/// <summary>
	/// True when at least one owner has <see cref="Attach" />'d this ViewModel
	/// (typically a View or a parent <see cref="DispatchableViewModel" />).
	/// AppDispatcher only applies changes while this is true. See <seealso cref="IAppDispatcher" />.
	/// </summary>
	[Notify]
	public partial bool IsAttached { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Applies this ViewModel's pending work, then asks each <see cref="TrackDispatchChild" />
	/// direct child to apply its own (which may flow further down). Override to add work;
	/// call <c> base.ApplyModelChanges() </c> when composing with bindings.
	/// </summary>
	public virtual void ApplyModelChanges()
	{
		ApplyPendingBindings();
		ApplyDispatchChildren();
	}

	/// <summary>
	/// Registers an attach owner (typically a View or parent ViewModel). Idempotent per owner.
	/// When the first owner attaches, nested dispatch children are also attached with this instance as owner.
	/// </summary>
	/// <param name="owner"> Owning view or parent ViewModel. Required — not null. </param>
	public void Attach(object owner)
	{
		ArgumentNullException.ThrowIfNull(owner);

		DispatchableViewModel[] childrenToAttach = null;
		var becameAttached = false;

		lock (_attachSync)
		{
			if (!_attachOwners.Add(owner))
			{
				return;
			}

			if (_attachOwners.Count == 1)
			{
				Volatile.Write(ref _isAttachedFlag, 1);
				becameAttached = true;
				if (_dispatchChildren is { Count: > 0 })
				{
					childrenToAttach = _dispatchChildren.ToArray();
				}
			}
		}

		if (!becameAttached)
		{
			return;
		}

		// Notify and cascade outside the lock (children lock themselves).
		IsAttached = true;

		if (childrenToAttach is null)
		{
			return;
		}

		foreach (var child in childrenToAttach)
		{
			child.Attach(this);
		}
	}

	/// <summary>
	/// Removes an attach owner. Idempotent per owner.
	/// When the last owner detaches, nested dispatch children are detached for this instance as owner.
	/// </summary>
	/// <param name="owner"> Owning view or parent ViewModel. Required — not null. </param>
	public void Detach(object owner)
	{
		ArgumentNullException.ThrowIfNull(owner);

		DispatchableViewModel[] childrenToDetach = null;
		var becameDetached = false;

		lock (_attachSync)
		{
			if (!_attachOwners.Remove(owner))
			{
				return;
			}

			if (_attachOwners.Count == 0)
			{
				Volatile.Write(ref _isAttachedFlag, 0);
				becameDetached = true;
				if (_dispatchChildren is { Count: > 0 })
				{
					childrenToDetach = _dispatchChildren.ToArray();
				}
			}
		}

		if (!becameDetached)
		{
			return;
		}

		IsAttached = false;

		if (childrenToDetach is null)
		{
			return;
		}

		foreach (var child in childrenToDetach)
		{
			child.Detach(this);
		}
	}

	/// <summary>
	/// True when this ViewModel or any attached direct dispatch child has work.
	/// Override to add checks; call <c> base.HasModelChanges() </c> when composing with bindings.
	/// </summary>
	public virtual bool HasModelChanges()
	{
		return HasPendingBindings() || HasDispatchChildModelChanges();
	}

	/// <summary>
	/// Stops cascading attach to a child and detaches this instance as an owner if needed.
	/// </summary>
	protected void ReleaseDispatchChild(DispatchableViewModel child)
	{
		if (child is null)
		{
			throw new ArgumentNullException(nameof(child));
		}

		lock (_attachSync)
		{
			if (_dispatchChildren is null || !_dispatchChildren.Remove(child))
			{
				return;
			}
		}

		child.Detach(this);
	}

	/// <summary>
	/// Registers a custom apply action driven by an <see cref="IDispatchPending" /> source.
	/// </summary>
	protected void TrackBinding(IDispatchPending pending, Action apply)
	{
		if (pending is null)
		{
			throw new ArgumentNullException(nameof(pending));
		}
		if (apply is null)
		{
			throw new ArgumentNullException(nameof(apply));
		}

		AddBinding(new PendingActionBinding(pending, apply));
	}

	/// <summary>
	/// Registers an arbitrary binding.
	/// </summary>
	protected void TrackBinding(IDispatchBinding binding)
	{
		if (binding is null)
		{
			throw new ArgumentNullException(nameof(binding));
		}

		AddBinding(binding);
	}

	/// <summary>
	/// Reconciles a destination list from a pending source list each dispatch tick.
	/// Source must implement <see cref="IDispatchPending" /> (e.g. <c> SpeedyList{T} </c>).
	/// </summary>
	protected void TrackCollection<TItem>(
		IList<TItem> source,
		IList<TItem> destination,
		IEqualityComparer<TItem> comparer = null,
		CollectionReconcileMode mode = CollectionReconcileMode.List)
	{
		if (source is null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if (destination is null)
		{
			throw new ArgumentNullException(nameof(destination));
		}
		if (source is not IDispatchPending pending)
		{
			throw new ArgumentException(
				$"Source must implement {nameof(IDispatchPending)} (e.g. SpeedyList).",
				nameof(source));
		}

		AddBinding(new CollectionDispatchBinding<TItem>(source, pending, destination, comparer, mode));
	}

	/// <summary>
	/// Registers a nested dispatchable managed by this ViewModel.
	/// When this instance becomes attached, children are <see cref="Attach" />'d with this as owner.
	/// If this instance is already attached, the child is attached immediately.
	/// </summary>
	protected T TrackDispatchChild<T>(T child) where T : DispatchableViewModel
	{
		if (child is null)
		{
			throw new ArgumentNullException(nameof(child));
		}

		var alreadyAttached = false;

		lock (_attachSync)
		{
			_dispatchChildren ??= [];
			if (_dispatchChildren.Contains(child))
			{
				return child;
			}

			_dispatchChildren.Add(child);
			alreadyAttached = Volatile.Read(ref _isAttachedFlag) != 0;
		}

		if (alreadyAttached)
		{
			child.Attach(this);
		}

		return child;
	}

	/// <summary>
	/// Drains a <see cref="TextIngress" /> into a consumer once per tick when pending.
	/// </summary>
	protected void TrackIngress(TextIngress source, Action<ReadOnlySpan<char>> consumer)
	{
		if (source is null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if (consumer is null)
		{
			throw new ArgumentNullException(nameof(consumer));
		}

		AddBinding(new IngressDispatchBinding(source, consumer));
	}

	/// <summary>
	/// Drains a <see cref="TextIngress" /> into an <see cref="IStringBuffer" /> once per tick when pending.
	/// </summary>
	protected void TrackIngress(TextIngress source, IStringBuffer destination)
	{
		if (source is null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if (destination is null)
		{
			throw new ArgumentNullException(nameof(destination));
		}

		AddBinding(new IngressDispatchBinding(source, destination.Append));
	}

	/// <summary>
	/// Map selected properties from an off-dispatcher model onto this ViewModel
	/// (optional two-way, rename, and type conversion). See <see cref="IPropertyMap" />.
	/// </summary>
	/// <param name="model"> Model that implements <see cref="ITrackPropertyChanges" /> (e.g. Keystone state / settings). </param>
	protected IPropertyMap TrackProperties(ITrackPropertyChanges model)
	{
		if (model is null)
		{
			throw new ArgumentNullException(nameof(model));
		}

		var binding = new PropertyMapBinding(model, model, this);
		AddBinding(binding);
		return binding;
	}

	private void AddBinding(IDispatchBinding binding)
	{
		_bindings ??= [];
		_bindings.Add(binding);
	}

	/// <summary>
	/// Direct children only — each child is responsible for its own children.
	/// </summary>
	private void ApplyDispatchChildren()
	{
		foreach (var child in SnapshotDispatchChildren())
		{
			if (child.IsAttached && child.HasModelChanges())
			{
				child.ApplyModelChanges();
			}
		}
	}

	private void ApplyPendingBindings()
	{
		if (_bindings is null)
		{
			return;
		}

		foreach (var binding in _bindings)
		{
			if (binding.HasPendingChanges())
			{
				binding.ApplyPendingChanges();
			}
		}
	}

	private bool HasDispatchChildModelChanges()
	{
		foreach (var child in SnapshotDispatchChildren())
		{
			if (child.IsAttached && child.HasModelChanges())
			{
				return true;
			}
		}

		return false;
	}

	private bool HasPendingBindings()
	{
		if (_bindings is null)
		{
			return false;
		}

		foreach (var binding in _bindings)
		{
			if (binding.HasPendingChanges())
			{
				return true;
			}
		}

		return false;
	}

	private DispatchableViewModel[] SnapshotDispatchChildren()
	{
		lock (_attachSync)
		{
			if (_dispatchChildren is not { Count: > 0 })
			{
				return [];
			}

			return _dispatchChildren.ToArray();
		}
	}

	#endregion

	#region Classes

	private sealed class CollectionDispatchBinding<TItem> : IDispatchBinding
	{
		#region Fields

		private readonly IEqualityComparer<TItem> _comparer;
		private readonly IList<TItem> _destination;
		private readonly CollectionReconcileMode _mode;
		private readonly IDispatchPending _pending;
		private readonly IList<TItem> _source;

		#endregion

		#region Constructors

		public CollectionDispatchBinding(
			IList<TItem> source,
			IDispatchPending pending,
			IList<TItem> destination,
			IEqualityComparer<TItem> comparer,
			CollectionReconcileMode mode)
		{
			_source = source;
			_pending = pending;
			_destination = destination;
			_comparer = comparer;
			_mode = mode;
		}

		#endregion

		#region Methods

		public void ApplyPendingChanges()
		{
			if (!_pending.HasPending)
			{
				return;
			}

			// Snapshot under concurrent producer mutation (Count + CopyTo is racy:
			// a shrink leaves trailing default/null slots that break Dictionary keys).
			var snapshot = SnapshotSource(_source);

			switch (_mode)
			{
				case CollectionReconcileMode.ListAndItems when _destination is IPresentationList<TItem> presentation:
				{
					presentation.ReconcileListAndItems(snapshot, _comparer);
					break;
				}
				case CollectionReconcileMode.ListAndItems:
				{
					_destination.ReconcileListAndItems(snapshot, _comparer);
					break;
				}
				case CollectionReconcileMode.List:
				default:
				{
					_destination.ReconcileList(snapshot, _comparer);
					break;
				}
			}

			_pending.ClearHasPending();
		}

		public bool HasPendingChanges()
		{
			return _pending.HasPending;
		}

		/// <summary>
		/// Best-effort consistent copy of a source list that may mutate on another thread.
		/// Retries when Count changes mid-copy; never returns trailing null slots from a shrink race.
		/// </summary>
		private static TItem[] SnapshotSource(IList<TItem> source)
		{
			if (source is SpeedyList<TItem> speedy)
			{
				for (var attempt = 0; attempt < 32; attempt++)
				{
					var count = speedy.Count;
					if (count == 0)
					{
						return [];
					}

					var result = new TItem[count];
					var span = speedy.AsSpan();
					if (span.Length != count)
					{
						continue;
					}

					span.CopyTo(result);

					if (speedy.Count != count)
					{
						continue;
					}

					return result;
				}

				// Last resort: materialize via enumerator without a pre-sized buffer.
				return [.. speedy];
			}

			for (var attempt = 0; attempt < 32; attempt++)
			{
				var count = source.Count;
				if (count == 0)
				{
					return [];
				}

				var result = new TItem[count];
				try
				{
					source.CopyTo(result, 0);
				}
				catch (ArgumentException)
				{
					// Source grew past the buffer mid-copy.
					continue;
				}

				if (source.Count != count)
				{
					continue;
				}

				// Drop trailing defaults if the source shrank after CopyTo started.
				if (default(TItem) is null)
				{
					var end = result.Length;
					while ((end > 0) && result[end - 1] is null)
					{
						end--;
					}

					if (end != result.Length)
					{
						Array.Resize(ref result, end);
					}
				}

				return result;
			}

			return [.. source];
		}

		#endregion
	}

	private sealed class IngressDispatchBinding : IDispatchBinding
	{
		#region Fields

		private readonly Action<ReadOnlySpan<char>> _consumer;
		private readonly TextIngress _source;

		#endregion

		#region Constructors

		public IngressDispatchBinding(TextIngress source, Action<ReadOnlySpan<char>> consumer)
		{
			_source = source;
			_consumer = consumer;
		}

		#endregion

		#region Methods

		public void ApplyPendingChanges()
		{
			_source.Drain(_consumer);
		}

		public bool HasPendingChanges()
		{
			return _source.HasPending;
		}

		#endregion
	}

	private sealed class PendingActionBinding : IDispatchBinding
	{
		#region Fields

		private readonly Action _apply;
		private readonly IDispatchPending _pending;

		#endregion

		#region Constructors

		public PendingActionBinding(IDispatchPending pending, Action apply)
		{
			_pending = pending;
			_apply = apply;
		}

		#endregion

		#region Methods

		public void ApplyPendingChanges()
		{
			if (!_pending.HasPending)
			{
				return;
			}

			_apply();
			_pending.ClearHasPending();
		}

		public bool HasPendingChanges()
		{
			return _pending.HasPending;
		}

		#endregion
	}

	#endregion
}