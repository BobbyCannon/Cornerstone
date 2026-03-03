#region References

using Cornerstone.Data;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Represents a hierarchy of data.
/// </summary>
public partial class SpeedyTree<T>
	: Notifiable, ISpeedyTree<T>
	where T : class, ISpeedyTree<T>
{
	#region Constructors

	public SpeedyTree()
	{
		Children = new SpeedyList<T>();
	}

	#endregion

	#region Properties

	public ISpeedyList<T> Children { get; }

	public T Parent { get; set; }

	#endregion
}

public interface ISpeedyTree<T>
	where T : class, ISpeedyTree<T>
{
	#region Properties

	public ISpeedyList<T> Children { get; }

	public T Parent { get; internal set; }

	#endregion

	#region Methods

	public T AddChild(T child)
	{
		child.Parent = (T) this;
		Children.Add(child);
		return child;
	}

	#endregion
}