#region References

using System;
using System.Collections.Generic;
using System.Threading;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
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
		using (BeginProjecting())
		{
			base.ApplyModelChanges();

			if (Model.HasChanges())
			{
				Model.ApplyChangesTo(this);
				Model.ResetHasChanges();
			}
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
	private Dictionary<string, Action> _intents;
	private int _isAttachedFlag;
	private int _projectingDepth;

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
	/// AppDispatcher only applies changes while this is true.
	/// </summary>
	[Notify]
	public partial bool IsAttached { get; private set; }

	/// <summary>
	/// True while <see cref="ApplyModelChanges" /> is running or an explicit
	/// <see cref="BeginProjecting" /> scope is open. <see cref="TrackIntent" />
	/// does not publish while this is true so apply copies do not echo as user intent.
	/// </summary>
	protected bool IsProjecting => _projectingDepth > 0;

	#endregion

	#region Methods

	/// <summary>
	/// Applies this ViewModel's pending work, then asks each <see cref="TrackDispatchChild" />
	/// direct child to apply its own (which may flow further down). Override to add work;
	/// call <c> base.ApplyModelChanges() </c> when composing with bindings.
	/// </summary>
	public virtual void ApplyModelChanges()
	{
		using (BeginProjecting())
		{
			ApplyPendingBindings();
			ApplyDispatchChildren();
		}
	}

	/// <summary>
	/// Drops every Track* / TrackIntent registration (this VM's apply recipe, not
	/// external event unsubscribe). UninitializeLifecycle already calls this.
	/// Call again only when re-binding to a new session/repo while still alive.
	/// </summary>
	protected void ReleaseTracks()
	{
		_bindings?.Clear();
		_intents?.Clear();
	}

	public override void UninitializeLifecycle()
	{
		ReleaseTracks();
		base.UninitializeLifecycle();
	}

	/// <summary>
	/// True when at least one Track* binding is registered.
	/// </summary>
	protected bool HasTracks => _bindings is { Count: > 0 };

	/// <summary>
	/// Suppresses <see cref="TrackIntent" /> publishes while view properties are written
	/// from State (apply, or a manual projection). Nested scopes are counted.
	/// </summary>
	protected ProjectingScope BeginProjecting()
	{
		_projectingDepth++;
		return new ProjectingScope(this);
	}

	/// <summary>
	/// Registers an "attach" owner (typically a View or parent ViewModel). Idempotent per owner.
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
		SyncApplyLoop(this, owner, true);

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
	/// Removes an "attach" owner. Idempotent per owner.
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
		SyncApplyLoop(this, owner, false);

		if (childrenToDetach is null)
		{
			return;
		}

		foreach (var child in childrenToDetach)
		{
			child.Detach(this);
		}
	}

	private static void SyncApplyLoop(DispatchableViewModel viewModel, object owner, bool include)
	{
		if (owner is DispatchableViewModel)
		{
			return;
		}

		IAppDispatcher dispatcher = owner as IAppDispatcher;
		if ((dispatcher == null)
			&& (AppBootstrap.DependencyProvider?.TryGetInstance<IAppDispatcher>(out var resolved) == true))
		{
			dispatcher = resolved;
		}

		if (dispatcher == null)
		{
			return;
		}

		if (include)
		{
			dispatcher.Track(viewModel);
		}
		else
		{
			dispatcher.Release(viewModel);
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
	/// Runs presentation work derived from already-tracked models (status text,
	/// selected-row match, tooltips). Applies on the first tick, then whenever
	/// another binding applies in the same tick. Does not consume pending flags.
	/// Register after the <c> Track* </c> calls this work depends on.
	/// </summary>
	protected void TrackDerived(Action apply)
	{
		if (apply is null)
		{
			throw new ArgumentNullException(nameof(apply));
		}

		AddBinding(new DerivedPresentationBinding(apply));
	}

	/// <summary>
	/// When the user changes the named ViewModel property, run publish (bus message).
	/// Assignments made during <see cref="ApplyModelChanges" /> or
	/// <see cref="BeginProjecting" /> do not publish.
	/// </summary>
	protected void TrackIntent(string propertyName, Action publish)
	{
		if (string.IsNullOrEmpty(propertyName))
		{
			throw new ArgumentException("Property name is required.", nameof(propertyName));
		}

		if (publish is null)
		{
			throw new ArgumentNullException(nameof(publish));
		}

		_intents ??= [];
		_intents[propertyName] = publish;
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
	/// Reconciles a presentation list from a pending source of a different item type
	/// (model row → row ViewModel). Source must implement IDispatchPending.
	/// create receives the source row. remove runs after a destination row is dropped.
	/// </summary>
	protected void TrackCollection<TSource, TDest>(
		IList<TSource> source,
		IList<TDest> destination,
		Func<TSource, TDest, bool> same,
		Func<TSource, TDest> create,
		Action<TDest, TSource> update,
		Action<TDest> remove)
	{
		if (source is null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if (destination is null)
		{
			throw new ArgumentNullException(nameof(destination));
		}
		if (same is null)
		{
			throw new ArgumentNullException(nameof(same));
		}
		if (create is null)
		{
			throw new ArgumentNullException(nameof(create));
		}
		if (update is null)
		{
			throw new ArgumentNullException(nameof(update));
		}
		if (remove is null)
		{
			throw new ArgumentNullException(nameof(remove));
		}
		if (source is not IDispatchPending pending)
		{
			throw new ArgumentException(
				$"Source must implement {nameof(IDispatchPending)} (e.g. SpeedyList).",
				nameof(source));
		}

		AddBinding(new ProjectedCollectionDispatchBinding<TSource, TDest>(
			source, pending, destination, same, create, update, remove));
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
	/// Copies a fixed-length model series into a view series when <see cref="ISeriesDataProvider.Version" /> drifts.
	/// Lengths must match (same contract as <see cref="SeriesDataProvider.CopyFrom" />).
	/// </summary>
	protected void TrackSeries(ISeriesDataProvider model, SeriesDataProvider view)
	{
		if (model is null)
		{
			throw new ArgumentNullException(nameof(model));
		}
		if (view is null)
		{
			throw new ArgumentNullException(nameof(view));
		}
		if (model.Length != view.Length)
		{
			throw new ArgumentException(
				$"Model length ({model.Length}) must match view length ({view.Length}).",
				nameof(view));
		}

		AddBinding(new FixedSeriesDispatchBinding(model, view));
	}

	/// <summary>
	/// Builds chart samples from a pending model source and publishes into a view series
	/// (same-length <see cref="SeriesDataProvider.ReplaceAll" />, or new provider + assign when length changes).
	/// </summary>
	protected void TrackSeries(
		IDispatchPending pending,
		Func<SeriesDataProvider> getView,
		Action<SeriesDataProvider> setView,
		Func<double[]> buildSamples)
	{
		if (pending is null)
		{
			throw new ArgumentNullException(nameof(pending));
		}
		if (getView is null)
		{
			throw new ArgumentNullException(nameof(getView));
		}
		if (setView is null)
		{
			throw new ArgumentNullException(nameof(setView));
		}
		if (buildSamples is null)
		{
			throw new ArgumentNullException(nameof(buildSamples));
		}

		AddBinding(new DerivedSeriesDispatchBinding(pending, getView, setView, buildSamples));
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

	/// <summary>
	/// Map every public property on <typeparamref name="TContract" /> that exists on both
	/// <paramref name="model" /> and <paramref name="view" />. Get-only members are one-way
	/// (model → view). Members with a setter are two-way. <paramref name="view" /> must be this instance.
	/// </summary>
	protected IPropertyMap TrackProperties<TContract>(TContract model, TContract view)
		where TContract : class
	{
		if (model is null)
		{
			throw new ArgumentNullException(nameof(model));
		}
		if (view is null)
		{
			throw new ArgumentNullException(nameof(view));
		}
		if (!ReferenceEquals(view, this))
		{
			throw new ArgumentException("View must be this ViewModel.", nameof(view));
		}
		if (model is not ITrackPropertyChanges changes)
		{
			throw new ArgumentException(
				$"Model must implement {nameof(ITrackPropertyChanges)}.",
				nameof(model));
		}

		var binding = new PropertyMapBinding(model, changes, this);
		AddBinding(binding);

		var contract = SourceReflector.GetSourceType(typeof(TContract));
		foreach (var property in contract.GetProperties())
		{
			if (property.IsIndexer
				|| property.IsStatic
				|| !property.CanRead
				|| !IsMappableContractPropertyType(property.PropertyInfo.PropertyType))
			{
				continue;
			}

			if (property.CanWrite)
			{
				binding.MapTwoWay(property.Name);
			}
			else
			{
				binding.MapOneWay(property.Name);
			}
		}

		return binding;
	}

	private static bool IsMappableContractPropertyType(Type type)
	{
		if (type == typeof(string))
		{
			return true;
		}

		if (type.IsEnum || type.IsPrimitive)
		{
			return true;
		}

		return (type == typeof(decimal))
			|| (type == typeof(DateTime))
			|| (type == typeof(DateTimeOffset))
			|| (type == typeof(Guid))
			|| (type == typeof(TimeSpan));
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		base.OnPropertyChanged(propertyName, oldValue, newValue);
		TryPublishIntent(propertyName);
	}

	private void AddBinding(IDispatchBinding binding)
	{
		_bindings ??= [];
		_bindings.Add(binding);
	}

	private void EndProjecting()
	{
		if (_projectingDepth > 0)
		{
			_projectingDepth--;
		}
	}

	private void TryPublishIntent(string propertyName)
	{
		if (IsProjecting || (_intents is null) || string.IsNullOrEmpty(propertyName))
		{
			return;
		}

		if (_intents.TryGetValue(propertyName, out var publish))
		{
			publish();
		}
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

		var applied = false;
		List<DerivedPresentationBinding> derived = null;

		foreach (var binding in _bindings)
		{
			if (binding is DerivedPresentationBinding derivedBinding)
			{
				derived ??= [];
				derived.Add(derivedBinding);
				continue;
			}

			if (binding.HasPendingChanges())
			{
				binding.ApplyPendingChanges();
				applied = true;
			}
		}

		if (derived is null)
		{
			return;
		}

		foreach (var binding in derived)
		{
			if (applied || binding.NeedsSeed)
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
			if (binding is DerivedPresentationBinding derived)
			{
				if (derived.NeedsSeed)
				{
					return true;
				}

				continue;
			}

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

			return [.. _dispatchChildren];
		}
	}

	#endregion

	#region Classes

	/// <summary>
	/// Ends a <see cref="BeginProjecting" /> scope. Nested scopes are counted.
	/// </summary>
	public readonly struct ProjectingScope : IDisposable
	{
		#region Fields

		private readonly DispatchableViewModel _owner;

		#endregion

		#region Constructors

		internal ProjectingScope(DispatchableViewModel owner)
		{
			_owner = owner;
		}

		#endregion

		#region Methods

		public void Dispose()
		{
			_owner?.EndProjecting();
		}

		#endregion
	}

	private sealed class ProjectedCollectionDispatchBinding<TSource, TDest> : IDispatchBinding
	{
		#region Fields

		private readonly Func<TSource, TDest> _create;
		private readonly IList<TDest> _destination;
		private readonly IDispatchPending _pending;
		private readonly Action<TDest> _remove;
		private readonly Func<TSource, TDest, bool> _same;
		private readonly IList<TSource> _source;
		private readonly Action<TDest, TSource> _update;

		#endregion

		#region Constructors

		public ProjectedCollectionDispatchBinding(
			IList<TSource> source,
			IDispatchPending pending,
			IList<TDest> destination,
			Func<TSource, TDest, bool> same,
			Func<TSource, TDest> create,
			Action<TDest, TSource> update,
			Action<TDest> remove)
		{
			_source = source;
			_pending = pending;
			_destination = destination;
			_same = same;
			_create = create;
			_update = update;
			_remove = remove;
		}

		#endregion

		#region Methods

		public void ApplyPendingChanges()
		{
			if (!_pending.HasPending)
			{
				return;
			}

			ReconcileProjected(_source, _destination, _same, _create, _update, _remove);
			_pending.ClearHasPending();
		}

		public bool HasPendingChanges()
		{
			return _pending.HasPending;
		}

		private static void ReconcileProjected(
			IList<TSource> source,
			IList<TDest> destination,
			Func<TSource, TDest, bool> same,
			Func<TSource, TDest> create,
			Action<TDest, TSource> update,
			Action<TDest> remove)
		{
			for (var i = 0; i < source.Count; i++)
			{
				var item = source[i];
				var destIndex = -1;
				for (var d = 0; d < destination.Count; d++)
				{
					if (same(item, destination[d]))
					{
						destIndex = d;
						break;
					}
				}

				if (destIndex < 0)
				{
					var row = create(item);
					update(row, item);
					if (i < destination.Count)
					{
						destination.Insert(i, row);
					}
					else
					{
						destination.Add(row);
					}

					continue;
				}

				if (destIndex != i)
				{
					var existing = destination[destIndex];
					destination.RemoveAt(destIndex);
					destination.Insert(Math.Min(i, destination.Count), existing);
					destIndex = i;
				}

				update(destination[destIndex], item);
			}

			while (destination.Count > source.Count)
			{
				var removed = destination[destination.Count - 1];
				destination.RemoveAt(destination.Count - 1);
				remove(removed);
			}
		}

		#endregion
	}

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

	/// <summary>
	/// Fixed-length model series → view via <see cref="SeriesDataProvider.CopyFrom" /> when versions differ.
	/// </summary>
	private sealed class FixedSeriesDispatchBinding : IDispatchBinding
	{
		#region Fields

		private readonly ISeriesDataProvider _model;
		private readonly SeriesDataProvider _view;

		#endregion

		#region Constructors

		public FixedSeriesDispatchBinding(ISeriesDataProvider model, SeriesDataProvider view)
		{
			_model = model;
			_view = view;
		}

		#endregion

		#region Methods

		public void ApplyPendingChanges()
		{
			if (_model.Version == _view.Version)
			{
				return;
			}

			_view.CopyFrom(_model);
		}

		public bool HasPendingChanges()
		{
			return _model.Version != _view.Version;
		}

		#endregion
	}

	/// <summary>
	/// Presentation derived from other Track* bindings. Applied after those bindings.
	/// </summary>
	private sealed class DerivedPresentationBinding : IDispatchBinding
	{
		#region Fields

		private readonly Action _apply;
		private bool _seeded;

		#endregion

		#region Constructors

		public DerivedPresentationBinding(Action apply)
		{
			_apply = apply;
		}

		#endregion

		#region Properties

		public bool NeedsSeed => !_seeded;

		#endregion

		#region Methods

		public void ApplyPendingChanges()
		{
			_apply();
			_seeded = true;
		}

		public bool HasPendingChanges()
		{
			return !_seeded;
		}

		#endregion
	}

	/// <summary>
	/// Pending source → build samples → <see cref="SeriesPresentation.Publish" /> into a view property.
	/// </summary>
	private sealed class DerivedSeriesDispatchBinding : IDispatchBinding
	{
		#region Fields

		private readonly Func<double[]> _buildSamples;
		private readonly Func<SeriesDataProvider> _getView;
		private readonly IDispatchPending _pending;
		private readonly Action<SeriesDataProvider> _setView;

		#endregion

		#region Constructors

		public DerivedSeriesDispatchBinding(
			IDispatchPending pending,
			Func<SeriesDataProvider> getView,
			Action<SeriesDataProvider> setView,
			Func<double[]> buildSamples)
		{
			_pending = pending;
			_getView = getView;
			_setView = setView;
			_buildSamples = buildSamples;
		}

		#endregion

		#region Methods

		public void ApplyPendingChanges()
		{
			if (!_pending.HasPending)
			{
				return;
			}

			SeriesPresentation.Publish(_buildSamples(), _getView(), _setView);
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