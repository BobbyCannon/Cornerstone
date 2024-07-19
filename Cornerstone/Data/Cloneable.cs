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
	/// <param name="options"> An optional set of included or excluded properties. </param>
	/// <returns> The cloned objects. </returns>
	public T DeepClone(int? maxDepth = null, IncludeExcludeOptions options = null);

	/// <summary>
	/// Shallow clone the item. No child items are cloned.
	/// </summary>
	/// <returns> The cloned objects. </returns>
	public T ShallowClone(IncludeExcludeOptions options = null);

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
	/// <param name="options"> An optional set of included or excluded properties. </param>
	/// <returns> The cloned objects. </returns>
	public object DeepCloneObject(int? maxDepth = null, IncludeExcludeOptions options = null);

	/// <summary>
	/// Shallow clone the item. No child items are cloned.
	/// </summary>
	/// <param name="options"> An optional set of included or excluded properties. </param>
	/// <returns> The cloned objects. </returns>
	public object ShallowCloneObject(IncludeExcludeOptions options = null);

	#endregion
}