#region References

using System.Collections.Generic;
using System.IO;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Models;

public class WebResourceResponse
{
	#region Properties

	public required Stream Content { get; set; }
	public required IDictionary<string, string> Headers { get; set; }
	public required int StatusCode { get; set; }
	public required string StatusMessage { get; set; }

	#endregion
}