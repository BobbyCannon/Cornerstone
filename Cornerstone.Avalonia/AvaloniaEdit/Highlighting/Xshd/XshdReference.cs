#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Highlighting.Xshd;

/// <summary>
/// A reference to an xshd color, or an inline xshd color.
/// </summary>
public struct XshdReference<T> : IEquatable<XshdReference<T>> where T : XshdElement
{
	#region Constructors

	/// <summary>
	/// Creates a new XshdReference instance.
	/// </summary>
	public XshdReference(string referencedDefinition, string referencedElement)
	{
		ReferencedDefinition = referencedDefinition;
		ReferencedElement = referencedElement ?? throw new ArgumentNullException(nameof(referencedElement));
		InlineElement = null;
	}

	/// <summary>
	/// Creates a new XshdReference instance.
	/// </summary>
	public XshdReference(T inlineElement)
	{
		ReferencedDefinition = null;
		ReferencedElement = null;
		InlineElement = inlineElement ?? throw new ArgumentNullException(nameof(inlineElement));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the inline element.
	/// </summary>
	public T InlineElement { get; }

	/// <summary>
	/// Gets the reference.
	/// </summary>
	public string ReferencedDefinition { get; }

	/// <summary>
	/// Gets the reference.
	/// </summary>
	public string ReferencedElement { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Applies the visitor to the inline element, if there is any.
	/// </summary>
	public object AcceptVisitor(IXshdVisitor visitor)
	{
		return InlineElement?.AcceptVisitor(visitor);
	}
	// The code in this region is useful if you want to use this structure in collections.
	// If you don't need it, you can just remove the region and the ": IEquatable<XshdColorReference>" declaration.

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		if (obj is XshdReference<T>)
		{
			return Equals((XshdReference<T>) obj); // use Equals method below
		}
		return false;
	}

	/// <summary>
	/// Equality operator.
	/// </summary>
	public bool Equals(XshdReference<T> other)
	{
		// add comparisions for all members here
		return (ReferencedDefinition == other.ReferencedDefinition)
			&& (ReferencedElement == other.ReferencedElement)
			&& (InlineElement == other.InlineElement);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		// combine the hash codes of all members here (e.g. with XOR operator ^)
		return GetHashCode(ReferencedDefinition) ^ GetHashCode(ReferencedElement) ^ GetHashCode(InlineElement);
	}

	/// <summary>
	/// Equality operator.
	/// </summary>
	public static bool operator ==(XshdReference<T> left, XshdReference<T> right)
	{
		return left.Equals(right);
	}

	/// <summary>
	/// Inequality operator.
	/// </summary>
	public static bool operator !=(XshdReference<T> left, XshdReference<T> right)
	{
		return !left.Equals(right);
	}

	private static int GetHashCode(object o)
	{
		return o != null ? o.GetHashCode() : 0;
	}

	#endregion
}