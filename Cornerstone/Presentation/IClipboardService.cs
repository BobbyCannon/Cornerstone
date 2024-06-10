#region References

using System.Threading.Tasks;

#endregion

namespace Cornerstone.Presentation;

/// <summary>
/// Contract for access the clipboard.
/// </summary>
public interface IClipboardService
{
	#region Methods

	/// <summary>
	/// Clear the clipboard.
	/// </summary>
	public Task ClearAsync();

	/// <summary>
	/// Get the text contained on the clipboard.
	/// </summary>
	/// <returns> The text from the clipboard. </returns>
	public Task<string> GetTextAsync();

	/// <summary>
	/// Update the clipboard with the provided text.
	/// </summary>
	/// <param name="text"> The text to set. </param>
	public Task SetTextAsync(string text);

	#endregion
}