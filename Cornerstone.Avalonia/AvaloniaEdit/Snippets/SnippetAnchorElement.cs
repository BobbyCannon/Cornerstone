#region References

#endregion

using Cornerstone.Text.Document;

namespace Cornerstone.Avalonia.AvaloniaEdit.Snippets;

/// <summary>
/// Creates a named anchor that can be accessed by other SnippetElements.
/// </summary>
public sealed class SnippetAnchorElement : SnippetElement
{
	#region Constructors

	/// <summary>
	/// Creates a SnippetAnchorElement with the supplied name.
	/// </summary>
	public SnippetAnchorElement(string name)
	{
		Name = name;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the name of the anchor.
	/// </summary>
	public string Name { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Insert(InsertionContext context)
	{
		var start = context.Document.CreateAnchor(context.InsertionPosition);
		start.MovementType = AnchorMovementType.BeforeInsertion;
		start.SurviveDeletion = true;
		var segment = new AnchorSegment(start, start);
		context.RegisterActiveElement(this, new AnchorElement(segment, Name, context));
	}

	#endregion
}

/// <summary>
/// AnchorElement created by SnippetAnchorElement.
/// </summary>
public sealed class AnchorElement : IActiveElement
{
	#region Fields

	private readonly InsertionContext _context;

	private AnchorSegment _segment;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new AnchorElement.
	/// </summary>
	public AnchorElement(AnchorSegment segment, string name, InsertionContext context)
	{
		_segment = segment;
		_context = context;
		Name = name;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public bool IsEditable => false;

	/// <summary>
	/// Gets or sets the name of the anchor.
	/// </summary>
	public string Name { get; }

	/// <inheritdoc />
	public ISegment Segment => _segment;

	/// <summary>
	/// Gets or sets the text at the anchor.
	/// </summary>
	public string Text
	{
		get => _context.Document.GetText(_segment);
		set
		{
			var offset = _segment.Offset;
			var length = _segment.Length;
			_context.Document.Replace(offset, length, value);
			if (length == 0)
			{
				// replacing an empty anchor segment with text won't enlarge it, so we have to recreate it
				_segment = new AnchorSegment(_context.Document, offset, value.Length);
			}
		}
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Deactivate(SnippetEventArgs e)
	{
	}

	/// <inheritdoc />
	public void OnInsertionCompleted()
	{
	}

	#endregion
}