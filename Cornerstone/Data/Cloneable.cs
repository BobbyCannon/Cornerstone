#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Represents a cloneable item.
/// </summary>
public interface ICloneable<out T> : ICloneable
{
	#region Methods

	/// <summary>
	/// Deep clone the item with child relationships. Default level is -1 which means clone full hierarchy of children.
	/// </summary>
	/// <param name="maxDepth"> The max depth to clone. Defaults to null. </param>
	/// <param name="settings"> An optional set of included or excluded properties. </param>
	/// <returns> The cloned objects. </returns>
	public T DeepClone(int? maxDepth = null, IncludeExcludeSettings settings = null);

	/// <summary>
	/// Shallow clone the item. No child items are cloned.
	/// </summary>
	/// <returns> The cloned objects. </returns>
	public T ShallowClone(IncludeExcludeSettings settings = null);

	#endregion
}

/// <summary>
/// Represents a cloneable item.
/// </summary>
public interface ICloneable
{
	#region Methods

	/// <summary>
	/// Deep clone the item with child relationships. Default level is -1 which means clone full hierarchy of children.
	/// </summary>
	/// <param name="maxDepth"> The max depth to clone. Defaults to null. </param>
	/// <param name="settings"> An optional set of included or excluded properties. </param>
	/// <returns> The cloned objects. </returns>
	public object DeepCloneObject(int? maxDepth = null, IncludeExcludeSettings settings = null);

	/// <summary>
	/// Shallow clone the item. No child items are cloned.
	/// </summary>
	/// <param name="settings"> An optional set of included or excluded properties. </param>
	/// <returns> The cloned objects. </returns>
	public object ShallowCloneObject(IncludeExcludeSettings settings = null);

	#endregion
}