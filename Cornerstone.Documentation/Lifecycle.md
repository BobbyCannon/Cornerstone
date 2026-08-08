# LifecycleTracker

LifecycleTracker is a hierarchical lifecycle manager.  
It owns a collection of child ILifecycle objects and coordinates their lifecycle phases so that the entire tree stays consistent.

It implements both ILifecycle (via CornerstoneObject) and ILifecycleTracker.

---

# Lifecycle 

```
Initialize
    ↓
  Load
    ↓
  Start
    ↓
[App Loop]
    └─► CanProces() -> Process()
    ↓
  Stop
    ↓
 Unload
    ↓
Uninitialize
```

---

## Core Principles

Direction | Phase                        | Order                          | Rationale
----------|------------------------------|--------------------------------|---------
Up        | Initialize → Load → Start    | Parent → Children              | Parent prepares the environment before children start using it.
Down      | Stop → Unload → Uninitialize | Children (LIFO) → Parent       | Children release resources before the parent tears down the environment.

- Children are processed in addition order on the way up.
- Children are processed in reverse addition order (stack / LIFO) on the way down.
- The tracker itself always runs after its children on the way down and before its children on the way up.

---

## Public API

### Tracking

T Track<T>(T child) where T : ILifecycle

- Adds the child to the internal list (idempotent – already-tracked children are ignored).
- Throws ArgumentNullException if child is null.
- Immediately advances the child to the current state of the tracker:
  - If the tracker is Initialized → child is Initialized
  - If the tracker is Loaded → child is Loaded
  - If the tracker is Started → child is Started
- Returns the same instance for fluent usage.

### Releasing

T Release<T>(T child) where T : ILifecycle

- Removes the child from the tracker (only if it was present).
- Fully tears the child down to the uninitialized state:
  1. Stop (if started)
  2. Unload (if loaded)
  3. Uninitialize (if initialized)
- Safe to call with null or with an object that was never tracked.
- Returns the same instance.

---

## Typical Usage Pattern

var tracker = new LifecycleTracker();

// Register children (can be done at any time)
var serviceA = tracker.Track(new MyServiceA());
var serviceB = tracker.Track(new MyServiceB());

// Normal lifecycle
tracker.InitializeLifecycle();   // Parent → A → B
tracker.LoadLifecycle();         // Parent → A → B
tracker.StartLifecycle();        // Parent → A → B

// ... application runs ...
//   [Process Loop]

// Late registration is supported
var serviceC = tracker.Track(new MyServiceC());  // C is initialized → loaded → started

// Explicit release
tracker.Release(serviceB);       // B is stopped → unloaded → uninitialized

// Shutdown
tracker.StopLifecycle();         // C → A → Parent
tracker.UnloadLifecycle();       // C → A → Parent
tracker.UninitializeLifecycle(); // C → A → Parent

---

## Important Guarantees

1. Consistency – A child is never left in a higher lifecycle state than its parent.
2. Idempotency – Calling Track multiple times on the same instance is safe.
3. Safe late binding – Children added after the parent has already started are automatically brought up to the correct state.
4. Deterministic teardown order – Children are always stopped/unloaded/uninitialized in reverse order of registration (LIFO).
5. No double-teardown – Release only acts on children that were actually tracked.

---

## Extension Points

- Children is protected, so derived trackers can inspect or manipulate the list if needed.
- OnChildTrack / OnChildRelease are the single places where state synchronization logic lives – override them if you need custom side-effects.

---

## Design Notes

This class follows the classic composite / hierarchical component pattern used by game engines, UI frameworks, and service hosts:

- Parent prepares the world → children start.
- Children finish their work → parent cleans up.

The reverse-order teardown is deliberate: it mirrors how objects are typically destroyed in a dependency hierarchy and reduces the chance of use-after-free style bugs between siblings.