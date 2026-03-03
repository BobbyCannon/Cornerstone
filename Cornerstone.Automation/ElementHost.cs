#region References

using Cornerstone.Extensions;
using System;
using System.Diagnostics;

#endregion

namespace Cornerstone.Automation;

/// <summary>
/// Represents a host for a set of elements.
/// </summary>
public abstract class ElementHost
{
	#region Constructors

	/// <summary>
	/// Initializes an instance of an element host.
	/// </summary>
	protected internal ElementHost(Application application, ElementHost parent)
	{
		Application = application;
		Children = new ElementCollection(parent);
		Parent = parent;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the application for this element host.
	/// </summary>
	public Application Application { get; protected set; }

	/// <summary>
	/// Gets a hierarchical list of elements.
	/// </summary>
	public ElementCollection Children { get; }

	/// <summary>
	/// Gets the ID of this element host.
	/// </summary>
	public abstract string Id { get; }

	/// <summary>
	/// Gets or sets the name of the element.
	/// </summary>
	public virtual string Name => Id;

	/// <summary>
	/// Gets the parent element of this element.
	/// </summary>
	public ElementHost Parent { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Get the child from the children.
	/// </summary>
	/// <param name="includeDescendants"> Flag to determine to search descendants or not. </param>
	/// <param name="wait"> Wait for the child to be available. Will auto refresh on each pass. </param>
	/// <returns> The child element for the condition or null if no child found. </returns>
	public T FirstOrDefault<T>(bool includeDescendants = true, bool wait = true) where T : Element
	{
		return FirstOrDefault<T>(x => true, includeDescendants, wait);
	}

	/// <summary>
	/// Get the child from the children.
	/// </summary>
	/// <param name="condition"> A function to test each element for a condition. </param>
	/// <param name="includeDescendants"> Flag to determine to search descendants or not. </param>
	/// <param name="wait"> Wait for the child to be available. Will auto refresh on each pass. </param>
	/// <returns> The child element for the condition or null if no child found. </returns>
	public T FirstOrDefault<T>(Func<T, bool> condition, bool includeDescendants = true, bool wait = true) where T : Element
	{
		T response = null;

		UtilityExtensions.WaitUntil(() =>
		{
			try
			{
				response = Children.FirstOrDefault(condition, includeDescendants);
				if ((response != null) || !wait)
				{
					return true;
				}

				Refresh(condition);
				return false;
			}
			catch (Exception)
			{
				return !wait;
			}
		});

		return response;
	}

	/// <summary>
	/// Refresh the children for this element host.
	/// </summary>
	public ElementHost Refresh()
	{
		return Refresh<Element>(x => false);
	}

	/// <summary>
	/// Refresh the children for this element host.
	/// </summary>
	public abstract ElementHost Refresh<T>(Func<T, bool> condition) where T : Element;

	#endregion
}