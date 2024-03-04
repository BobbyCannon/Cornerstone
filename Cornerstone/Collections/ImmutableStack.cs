#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

#endregion

// ReSharper disable once CheckNamespace
namespace Cornerstone.Collections;

/// <summary>
/// An immutable stack. Iterating the stack will return the items from top to bottom (in the order they would be popped).
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public sealed class ImmutableStack<T> : IEnumerable<T>
{
    #region Fields

    /// <summary>
    /// Gets the empty stack instance.
    /// </summary>
    public static readonly ImmutableStack<T> Empty = new();
    private readonly ImmutableStack<T> _next;
    private readonly T _value;

    #endregion

    #region Constructors

    private ImmutableStack()
    {
    }

    private ImmutableStack(T value, ImmutableStack<T> next)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        _next = next;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets if this stack is empty.
    /// </summary>
    public bool IsEmpty => _next == null;

    #endregion

    #region Methods

    /// <summary>
    /// Gets an enumerator that iterates through the stack top-to-bottom.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        var t = this;
        while (!t.IsEmpty)
        {
            yield return t._value;
            t = t._next;
        }
    }

    /// <summary>
    /// Gets the item on the top of the stack.
    /// </summary>
    /// <exception cref="InvalidOperationException"> The stack is empty. </exception>
    public T Peek()
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Operation not valid on empty stack.");
        }

        return _value;
    }

    /// <summary>
    /// Gets the item on the top of the stack.
    /// Returns <c> default(T) </c> if the stack is empty.
    /// </summary>
    public T PeekOrDefault()
    {
        return _value;
    }

    /// <summary>
    /// Gets the stack with the top item removed.
    /// </summary>
    /// <exception cref="InvalidOperationException"> The stack is empty. </exception>
    public ImmutableStack<T> Pop()
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Operation not valid on empty stack.");
        }

        return _next;
    }

    /// <summary>
    /// Pushes an item on the stack. This does not modify the stack itself, but returns a new
    /// one with the value pushed.
    /// </summary>
    public ImmutableStack<T> Push(T item)
    {
        return new ImmutableStack<T>(item, this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var b = new StringBuilder("[Stack");
        foreach (var val in this)
        {
            b.Append(' ');
            b.Append(val);
        }

        b.Append(']');
        return b.ToString();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}