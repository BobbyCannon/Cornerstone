#region References

using System.Threading.Tasks;
using Cornerstone.Collections;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Automation.Web;

public interface IElement<T> : IHierarchySyncItem
	where T : IHierarchySyncItem
{
	#region Properties

	/// <summary>
	/// Gets the ID of this element host.
	/// </summary>
	public string Id { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Set focus on the element.
	/// </summary>
	public Task FocusAsync();

	#endregion
}