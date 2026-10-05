#region References

using System;
using System.Collections.Generic;
using System.Threading;
using Cornerstone.Extensions;
using Cornerstone.Input;
using Cornerstone.Platforms.Windows;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Automation;

/// <summary>
/// Represents a host for a set of elements.
/// </summary>
public abstract class ElementHost : IDisposable
{
	#region Fields

	private Keyboard _keyboard;
	private Mouse _mouse;
	private IDateTimeProvider _timeProvider;

	#endregion

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
	/// Gets the current focused element.
	/// </summary>
	public abstract Element FocusedElement { get; }

	/// <summary>
	/// Gets the ID of this element host.
	/// </summary>
	public abstract string Id { get; }

	/// <summary>
	/// Keyboard used to send input. Defaults to the application keyboard, then WindowsInput.Keyboard.
	/// </summary>
	public Keyboard Keyboard
	{
		get
		{
			if (_keyboard != null)
			{
				return _keyboard;
			}

			if ((Application != null) && !ReferenceEquals(Application, this))
			{
				return Application.Keyboard;
			}

			return Parent?.Keyboard ?? WindowsInput.Keyboard;
		}
		set => _keyboard = value;
	}

	/// <summary>
	/// Mouse used to send input. Defaults to the application mouse, then WindowsInput.Mouse.
	/// </summary>
	public Mouse Mouse
	{
		get
		{
			if (_mouse != null)
			{
				return _mouse;
			}

			if ((Application != null) && !ReferenceEquals(Application, this))
			{
				return Application.Mouse;
			}

			return Parent?.Mouse ?? WindowsInput.Mouse;
		}
		set => _mouse = value;
	}

	/// <summary>
	/// Time source for waits and delays. Defaults to the application provider, then real time.
	/// </summary>
	public IDateTimeProvider TimeProvider
	{
		get
		{
			if (_timeProvider != null)
			{
				return _timeProvider;
			}

			if ((Application != null) && !ReferenceEquals(Application, this))
			{
				return Application.TimeProvider;
			}

			return Parent?.TimeProvider ?? DateTimeProvider.RealTime;
		}
		set => _timeProvider = value;
	}

	/// <summary>
	/// Gets or sets the name of the element.
	/// </summary>
	public virtual string Name => Id;

	/// <summary>
	/// Gets the parent element of this element.
	/// </summary>
	public ElementHost Parent { get; internal set; }

	/// <summary>
	/// Timeout used when waiting for an element to appear. Uses Application.Timeout when an
	/// application is available, otherwise Application.DefaultTimeout.
	/// </summary>
	protected virtual int SearchTimeoutMilliseconds =>
		Application != null
			? (int) Application.Timeout.TotalMilliseconds
			: Application.DefaultTimeout;

	/// <summary>
	/// True when ID lookups can query the native tree (UIA or CSS) without a full refresh.
	/// </summary>
	protected virtual bool SupportsNativeIdSearch => false;

	/// <summary>
	/// True when predicate lookups can query the native tree without a full refresh.
	/// </summary>
	protected virtual bool SupportsNativeSearch => false;

	/// <summary>
	/// The all parent element of this element.
	/// </summary>
	public IEnumerable<ElementHost> Parents
	{
		get
		{
			var parent = Parent;
			var response = new List<ElementHost>();

			while (parent != null)
			{
				response.Add(parent);
				parent = parent.Parent;
			}

			return response;
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Check to see if this element host contains an element.
	/// </summary>
	/// <param name="id"> The id to search for. </param>
	/// <returns> True if the id is found, false if otherwise. </returns>
	public bool Contains(string id)
	{
		return FirstOrDefault(id, true, false) != null;
	}

	/// <summary>
	/// Get all the children.
	/// </summary>
	/// <returns> The child elements. </returns>
	public IEnumerable<Element> Descendants()
	{
		return Children.Descendants(x => true);
	}

	/// <summary>
	/// Get all the children that match the optional condition. If a condition is not provided
	/// then all children of the type will be returned.
	/// </summary>
	/// <param name="condition"> A function to test each element for a condition. </param>
	/// <returns> The child elements for the condition. </returns>
	public IEnumerable<Element> Descendants(Func<Element, bool> condition)
	{
		return Children.Descendants(condition);
	}

	/// <summary>
	/// Get all the children of a specific type.
	/// </summary>
	/// <returns> The child elements of the specific type. </returns>
	public IEnumerable<T> Descendants<T>() where T : Element
	{
		return Descendants<T>(x => true);
	}

	/// <summary>
	/// Get all the children of a specific type that matches the condition.
	/// </summary>
	/// <param name="condition"> A function to test each element for a condition. </param>
	/// <returns> The child elements of the specific type for the condition. </returns>
	public IEnumerable<T> Descendants<T>(Func<T, bool> condition) where T : Element
	{
		return Children.Descendants(condition);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Get the child from the children.
	/// </summary>
	/// <param name="id"> An ID of the element to get. </param>
	/// <param name="includeDescendants"> Flag to determine to search descendants or not. </param>
	/// <param name="wait"> Wait for the child to be available. Will auto refresh on each pass. </param>
	/// <remarks>
	/// The First method throws an exception if source contains no elements. To instead return a default value
	/// when the source sequence is empty, use the FirstOrDefault method.
	/// </remarks>
	/// <returns> The child element for the ID. </returns>
	public Element First(string id, bool includeDescendants = true, bool wait = true)
	{
		return First<Element>(id, includeDescendants, wait);
	}

	/// <summary>
	/// Get the child from the children.
	/// </summary>
	/// <param name="condition"> A function to test each element for a condition. </param>
	/// <param name="includeDescendants"> Flag to determine to search descendants or not. </param>
	/// <param name="wait"> Wait for the child to be available. Will auto refresh on each pass. </param>
	/// <remarks>
	/// The First method throws an exception if source contains no elements. To instead return a default value
	/// when the source sequence is empty, use the FirstOrDefault method.
	/// </remarks>
	/// <returns> The child element for the condition or null if no child found. </returns>
	public Element First(Func<Element, bool> condition, bool includeDescendants = true, bool wait = true)
	{
		return First<Element>(condition, includeDescendants, wait);
	}

	/// <summary>
	/// Get the child from the children.
	/// </summary>
	/// <param name="includeDescendants"> Flag to determine to search descendants or not. </param>
	/// <param name="wait"> Wait for the child to be available. Will auto refresh on each pass. </param>
	/// <remarks>
	/// The First method throws an exception if source contains no elements. To instead return a default value
	/// when the source sequence is empty, use the FirstOrDefault method.
	/// </remarks>
	/// <returns> The child element for the ID. </returns>
	public T First<T>(bool includeDescendants = true, bool wait = true) where T : Element
	{
		return First<T>(x => true, includeDescendants, wait);
	}

	/// <summary>
	/// Get the child from the children.
	/// </summary>
	/// <param name="id"> An ID of the element to get. </param>
	/// <param name="includeDescendants"> Flag to determine to search descendants or not. </param>
	/// <param name="wait"> Wait for the child to be available. Will auto refresh on each pass. </param>
	/// <remarks>
	/// The First method throws an exception if source contains no elements. To instead return a default value
	/// when the source sequence is empty, use the FirstOrDefault method.
	/// </remarks>
	/// <returns> The child element for the ID. </returns>
	public T First<T>(string id, bool includeDescendants = true, bool wait = true) where T : Element
	{
		return First(GetDefaultLookupPredicate<T>(id), includeDescendants, wait);
	}

	/// <summary>
	/// Get the child from the children.
	/// </summary>
	/// <param name="condition"> A function to test each element for a condition. </param>
	/// <param name="includeDescendants"> Flag to determine to search descendants or not. </param>
	/// <param name="wait"> Wait for the child to be available. Will auto refresh on each pass. </param>
	/// <remarks>
	/// The First method throws an exception if source contains no elements. To instead return a default value
	/// when the source sequence is empty, use the FirstOrDefault method.
	/// </remarks>
	/// <exception cref="InvalidOperationException"> The source sequence is empty. </exception>
	/// <returns> The child element for the condition. </returns>
	public T First<T>(Func<T, bool> condition, bool includeDescendants = true, bool wait = true) where T : Element
	{
		var response = FirstOrDefault(condition, includeDescendants, wait);

		if (response == null)
		{
			throw new InvalidOperationException("The source sequence is empty.");
		}

		return response;
	}

	/// <summary>
	/// Get the child from the children.
	/// </summary>
	/// <param name="id"> An ID of the element to get. </param>
	/// <param name="includeDescendants"> Flag to determine to search descendants or not. </param>
	/// <param name="wait"> Wait for the child to be available. Will auto refresh on each pass. </param>
	/// <returns> The child element for the ID or null if no child found. </returns>
	public Element FirstOrDefault(string id, bool includeDescendants = true, bool wait = true)
	{
		return FirstOrDefault<Element>(id, includeDescendants, wait);
	}

	/// <summary>
	/// Get the child from the children.
	/// </summary>
	/// <param name="condition"> A function to test each element for a condition. </param>
	/// <param name="includeDescendants"> Flag to determine to search descendants or not. </param>
	/// <param name="wait"> Wait for the child to be available. Will auto refresh on each pass. </param>
	/// <returns> The child element for the condition or null if no child found. </returns>
	public Element FirstOrDefault(Func<Element, bool> condition, bool includeDescendants = true, bool wait = true)
	{
		return FirstOrDefault<Element>(condition, includeDescendants, wait);
	}

	/// <summary>
	/// Get the child from the children.
	/// </summary>
	/// <param name="id"> An ID of the element to get. </param>
	/// <param name="includeDescendants"> Flag to determine to search descendants or not. </param>
	/// <param name="wait"> Wait for the child to be available. Will auto refresh on each pass. </param>
	/// <returns> The child element for the ID or null if no child found. </returns>
	public T FirstOrDefault<T>(string id, bool includeDescendants = true, bool wait = true) where T : Element
	{
		var predicate = GetDefaultLookupPredicate<T>(id);
		return SearchUntil(
			() => FindNativeById<T>(id, includeDescendants) ?? Children.FirstOrDefault(predicate, includeDescendants),
			() =>
			{
				if (!SupportsNativeIdSearch)
				{
					Refresh(predicate);
				}
			},
			wait
		);
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
		return SearchUntil(
			() => Children.FirstOrDefault(condition, includeDescendants) ?? FindNative(condition, includeDescendants),
			() =>
			{
				if (!SupportsNativeSearch)
				{
					Refresh(condition);
				}
			},
			wait
		);
	}

	/// <summary>
	/// Get the child from the children.
	/// </summary>
	/// <param name="includeDescendants"> Flag to determine to search descendants or not. </param>
	/// <param name="wait"> Wait for the child to be available. Will auto refresh on each pass. </param>
	/// <returns> The child element for the condition or null if no child found. </returns>
	public T FirstOrDefault<T>(bool includeDescendants = true, bool wait = true) where T : Element
	{
		return SearchUntil(
			() => Children.FirstOrDefault<T>(includeDescendants) ?? FindNative<T>(x => true, includeDescendants),
			() =>
			{
				if (!SupportsNativeSearch)
				{
					Refresh<T>(y => true);
				}
			},
			wait
		);
	}

	/// <summary>
	/// Gets a collection of element of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type of the element for the collection. </typeparam>
	/// <returns> The collection of elements of the provided type. </returns>
	public IEnumerable<T> OfType<T>() where T : Element
	{
		return Children.OfType<T>();
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

	/// <summary>
	/// Removes an element from a collection.
	/// </summary>
	/// <param name="element"> The element to be removed. </param>
	/// <param name="includeDescendants"> The flag that determines to search descendants or not. </param>
	/// <returns> true if item is successfully removed otherwise false. This method also returns false if item was not found. </returns>
	public bool Remove<T>(T element, bool includeDescendants = true) where T : Element
	{
		return Children.Remove(element, includeDescendants);
	}

	/// <summary>
	/// Waits for the host to complete the work. Will wait until no longer busy.
	/// </summary>
	/// <param name="minimumDelay"> The minimum delay in milliseconds to wait. Defaults to 0 milliseconds. </param>
	public abstract ElementHost WaitForComplete(int minimumDelay = 0);

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	/// <summary>
	/// Pauses for the provided milliseconds when using real time. Test clocks skip the block.
	/// </summary>
	protected void Delay(int milliseconds)
	{
		if (milliseconds <= 0)
		{
			return;
		}

		if (ReferenceEquals(TimeProvider, DateTimeProvider.RealTime))
		{
			Thread.Sleep(milliseconds);
		}
	}

	protected abstract void Dispose(bool disposing);

	/// <summary>
	/// Native tree search for an element matching the condition. Default is no native search.
	/// </summary>
	protected virtual T FindNative<T>(Func<T, bool> condition, bool includeDescendants) where T : Element
	{
		return default;
	}

	/// <summary>
	/// Native tree search by automation id, id, or name. Default is no native search.
	/// </summary>
	protected virtual T FindNativeById<T>(string id, bool includeDescendants) where T : Element
	{
		return default;
	}

	private static Func<T, bool> GetDefaultLookupPredicate<T>(string id) where T : Element
	{
		return x =>
			(x.AutomationId == id)
			|| (x.FullId == id)
			|| (x.Id == id)
			|| (x.Name == id);
	}

	private T SearchUntil<T>(Func<T> search, Action retry, bool wait) where T : Element
	{
		T response = null;

		this.WaitUntil(_ =>
		{
			try
			{
				response = search();
				if ((response != null) || !wait)
				{
					return true;
				}

				retry?.Invoke();
				return false;
			}
			catch (Exception)
			{
				return !wait;
			}
		}, SearchTimeoutMilliseconds, 10, timeProvider: TimeProvider);

		return response;
	}

	#endregion
}