#region References

using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting.Xshd;

/// <summary>
/// A color in an Xshd file.
/// </summary>
public class XshdColor : XshdElement
{
	#region Properties

	/// <summary>
	/// Gets/sets the background brush.
	/// </summary>
	public HighlightingBrush Background { get; set; }

	/// <summary>
	/// Gets/Sets the example text that demonstrates where the color is used.
	/// </summary>
	public string ExampleText { get; set; }

	/// <summary>
	/// Gets/sets the font family
	/// </summary>
	public FontFamily FontFamily { get; set; }

	/// <summary>
	/// Gets/sets the font size.
	/// </summary>
	public int? FontSize { get; set; }

	/// <summary>
	/// Gets/sets the font style.
	/// </summary>
	public FontStyle? FontStyle { get; set; }

	/// <summary>
	/// Gets/sets the font weight.
	/// </summary>
	public FontWeight? FontWeight { get; set; }

	/// <summary>
	/// Gets/sets the foreground brush.
	/// </summary>
	public HighlightingBrush Foreground { get; set; }

	/// <summary>
	/// Gets/sets the name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets/sets the strikethrough flag
	/// </summary>
	public bool? Strikethrough { get; set; }

	/// <summary>
	/// Gets/sets the underline flag
	/// </summary>
	public bool? Underline { get; set; }

	#endregion

	#region Methods

	///// <summary>
	///// Serializes this XshdColor instance.
	///// </summary>
	//public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	//{
	//	if (info == null)
	//		throw new ArgumentNullException("info");
	//	info.AddValue("Name", this.Name);
	//	info.AddValue("Foreground", this.Foreground);
	//	info.AddValue("Background", this.Background);
	//	info.AddValue("HasUnderline", this.Underline.HasValue);
	//	if (this.Underline.HasValue)
	//		info.AddValue("Underline", this.Underline.Value);
	//	info.AddValue("HasWeight", this.FontWeight.HasValue);
	//	if (this.FontWeight.HasValue)
	//		info.AddValue("Weight", this.FontWeight.Value.ToOpenTypeWeight());
	//	info.AddValue("HasStyle", this.FontStyle.HasValue);
	//	if (this.FontStyle.HasValue)
	//		info.AddValue("Style", this.FontStyle.Value.ToString());
	//	info.AddValue("ExampleText", this.ExampleText);
	//}

	/// <inheritdoc />
	public override object AcceptVisitor(IXshdVisitor visitor)
	{
		return visitor.VisitColor(this);
	}

	#endregion
}