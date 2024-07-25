#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Shared;

public interface IPropertyMapper
{
	#region Methods

	IEnumerable<string> GetKeys();

	#endregion
}