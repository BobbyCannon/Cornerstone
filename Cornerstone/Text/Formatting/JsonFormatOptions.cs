#region References

using System.ComponentModel;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Text.Formatting;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class JsonFormatOptions : DocumentFormatOptions
{
	#region Constructors

	public JsonFormatOptions()
	{
		SpaceAfterColon = true;
	}

	#endregion

	#region Properties

	[Category("Formatting")]
	[DisplayName("Minify")]
	[Description("Remove optional whitespace.")]
	[Notify]
	public partial bool Minify { get; set; }

	[Category("Formatting")]
	[DisplayName("Space After Colon")]
	[Description("Insert a space after ':' in objects.")]
	[Notify]
	public partial bool SpaceAfterColon { get; set; }

	#endregion
}