#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Models;

public record WebScheme(string Scheme, string AppAddress, Uri BaseUri)
{
}