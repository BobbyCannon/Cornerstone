#region References

using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

public partial class Token : Notifiable<Token>
{
	#region Properties

	/// <summary>
	/// The length of the token.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Length { get; set; }

	/// <summary>
	/// The offset (start) of the token.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Offset { get; set; }

	#endregion
}