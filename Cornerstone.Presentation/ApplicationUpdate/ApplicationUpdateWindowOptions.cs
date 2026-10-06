#region References

using System.Reflection;

#endregion

namespace Cornerstone.Presentation.ApplicationUpdate;

/// <summary>
/// Chrome for <see cref="ApplicationUpdateWindow" />. The copy itself always reads <c> -Update </c>.
/// </summary>
public sealed class ApplicationUpdateWindowOptions
{
	#region Properties

	/// <summary>
	/// Assembly that contains <see cref="ProductImage" /> and <see cref="WindowIcon" />.
	/// </summary>
	public Assembly ImageAssembly { get; set; }

	/// <summary>
	/// Cornerstone resource path for the image shown above the status (for example <c> /Assets/App.png </c>).
	/// </summary>
	public string ProductImage { get; set; }

	/// <summary>
	/// Window title. When empty, the window keeps its default title.
	/// </summary>
	public string Title { get; set; }

	/// <summary>
	/// Cornerstone resource path for the window icon (for example <c> /Assets/App.ico </c>).
	/// </summary>
	public string WindowIcon { get; set; }

	#endregion
}