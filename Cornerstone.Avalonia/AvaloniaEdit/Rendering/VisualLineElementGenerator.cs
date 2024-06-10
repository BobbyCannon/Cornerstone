#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Rendering;

/// <summary>
/// Abstract base class for generators that produce new visual line elements.
/// </summary>
public abstract class VisualLineElementGenerator
{
	#region Fields

	/// <summary>
	/// Should only be used by VisualLine.ConstructVisualElements.
	/// </summary>
	internal int CachedInterest;

	#endregion

	#region Properties

	/// <summary>
	/// Gets the text run construction context.
	/// </summary>
	protected ITextRunConstructionContext CurrentContext { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Constructs an element at the specified offset.
	/// May return null if no element should be constructed.
	/// </summary>
	/// <remarks>
	/// Avoid signalling interest and then building no element by returning null - doing so
	/// causes the generated <see cref="VisualLineText" /> elements to be unnecessarily split
	/// at the position where you signalled interest.
	/// </remarks>
	public abstract VisualLineElement ConstructElement(int offset);

	/// <summary>
	/// De-initializes the generator.
	/// </summary>
	public virtual void FinishGeneration()
	{
		CurrentContext = null;
	}

	/// <summary>
	/// Gets the first offset >= startOffset where the generator wants to construct an element.
	/// Return -1 to signal no interest.
	/// </summary>
	public abstract int GetFirstInterestedOffset(int startOffset);

	/// <summary>
	/// Initializes the generator for the <see cref="ITextRunConstructionContext" />
	/// </summary>
	public virtual void StartGeneration(ITextRunConstructionContext context)
	{
		CurrentContext = context ?? throw new ArgumentNullException(nameof(context));
	}

	#endregion
}

internal interface IBuiltinElementGenerator
{
	#region Methods

	void FetchOptions(TextEditorOptions options);

	#endregion
}