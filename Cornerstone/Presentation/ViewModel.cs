#region References

using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a view model.
/// </summary>
public abstract class ViewModel<T> : ViewModel, IViewModel<T>
{
	#region Properties

	public T Id { get; set; }

	#endregion
}

/// <summary>
/// Represents a view model.
/// </summary>
[SourceReflection]
public abstract partial class ViewModel : CornerstoneObject, IViewModel
{
}

public interface IViewModel<T> : IViewModel
{
	#region Properties

	/// <summary>
	/// Gets or sets the ID of the view.
	/// </summary>
	public T Id { get; set; }

	#endregion
}

/// <summary>
/// Represents an object that subscribes to events.
/// </summary>
public interface IViewModel : ILifecycle
{
}