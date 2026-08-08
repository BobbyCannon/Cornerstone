## View Integration

Keystone is designed to serve as the **Model** layer in a classic MVVM architecture.

Because the Keystone **State** is the single source of truth and is deliberately free of UI concerns, any UI framework can attach to it as the Model.  
ViewModels then become thin, reactive adapters that expose the State (or projected slices of it) in a form that is convenient for binding to Views.

This keeps the domain logic, mutation rules, and communication completely isolated from the presentation layer while still allowing the UI to stay perfectly in sync with the underlying State.

### Cornerstone Dispatcher

The **Cornerstone Dispatcher** is the bridge that connects Keystone’s State to the MVVM world.

It runs on a **hard, deterministic loop** (similar to a game engine’s update loop).  
Every tick the Dispatcher walks the registered ViewModels and updates only those that are currently marked **Active**. Inactive ViewModels are completely skipped, keeping the loop extremely fast and predictable.

#### Core Responsibilities

- **Hard Update Loop** – A fixed, deterministic cycle that drives all ViewModel updates
- **Active Filtering** – Only ViewModels marked as `Active` are processed each frame/tick
- **Projection & Sync** – Transform Keystone State into ViewModel properties
- **Lifecycle Management** – Handle activation, deactivation, and cleanup of ViewModels

#### Typical Flow (per tick)

1. The Cornerstone Dispatcher begins its hard update loop.
2. It iterates only over ViewModels that are currently marked **Active**.
3. For each active ViewModel it pulls the latest relevant data from the Keystone State.
4. It applies any required projections/transformations.
5. It writes the results into the ViewModel (which is then bound to the View).
6. Inactive ViewModels are skipped entirely, ensuring maximum performance.

### Wiring Model to the ViewModel

Most Model-to-ViewModel connections can be established automatically through the `IUpdateable<T>` interface.

Any ViewModel that implements `IUpdateable<T>` (where `T` is a type present in the Keystone State) can be auto-wired by the Cornerstone Dispatcher. The Dispatcher will:

- Detect the matching State slice of type `T`
- Keep the ViewModel in sync with that slice every tick (when the ViewModel is Active)
- Require almost zero manual configuration for simple one-to-one mappings

More complex projections or multi-source ViewModels can still be registered manually when needed.