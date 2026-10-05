#region References

using System.ComponentModel;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.Formatting;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class XmlFormatOptions : DocumentFormatOptions
{
	#region Constructors

	public XmlFormatOptions()
	{
		SpaceBeforeEmptyClose = true;
		Dialect = XmlFormatDialect.Xml;
	}

	#endregion

	#region Properties

	[Category("Formatting")]
	[DisplayName("Attributes On New Line")]
	[Description("Put each attribute on its own indented line.")]
	[Notify]
	public partial bool AttributesOnNewLine { get; set; }

	[Category("Formatting")]
	[DisplayName("Dialect")]
	[Description("Xml pretty-prints markup; Html keeps void elements and preformatted content.")]
	[Notify]
	public partial XmlFormatDialect Dialect { get; set; }

	[Category("Formatting")]
	[DisplayName("Minify")]
	[Description("Remove optional whitespace.")]
	[Notify]
	public partial bool Minify { get; set; }

	[Category("Formatting")]
	[DisplayName("Space Before Empty Close")]
	[Description("Write a space before '/>' on empty elements.")]
	[Notify]
	public partial bool SpaceBeforeEmptyClose { get; set; }

	#endregion
}