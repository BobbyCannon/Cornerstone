using System;
using System.Collections.Generic;

namespace Cornerstone.VisualStudio.Core.Preview;

/// <summary>
/// Distinguishes ASP.NET / Blazor host projects from desktop apps that happen to
/// reference a Microsoft.AspNetCore.* client package (e.g. SignalR.Client).
/// </summary>
public static class WebProjectDetection
{
	#region Methods

	public static bool HasDesktopUiStack(IReadOnlyList<string> references)
	{
		return Contains(references, "Avalonia.Desktop")
			|| Contains(references, "Avalonia.Win32")
			|| Contains(references, "Avalonia.Native")
			|| Contains(references, "Avalonia.X11")
			|| Contains(references, "Cornerstone.Presentation");
	}

	/// <summary>
	/// Server / web-host ASP.NET stacks. Client packages such as SignalR.Client do not count.
	/// </summary>
	public static bool HasAspNetWebServerReferences(IReadOnlyList<string> references)
	{
		if ((references == null) || (references.Count == 0))
		{
			return false;
		}

		for (var i = 0; i < references.Count; i++)
		{
			if (IsAspNetWebServerAssembly(references[i]))
			{
				return true;
			}
		}

		return false;
	}

	public static bool IsAspNetWebServerAssembly(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}

		if (string.Equals(name, "Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(name, "Microsoft.AspNetCore.App", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (!name.StartsWith("Microsoft.AspNetCore.", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		// Client libraries used by desktop / mobile apps.
		if (name.IndexOf("SignalR.Client", StringComparison.OrdinalIgnoreCase) >= 0 ||
			name.IndexOf("Http.Connections.Client", StringComparison.OrdinalIgnoreCase) >= 0 ||
			name.IndexOf(".Client", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return false;
		}

		return name.StartsWith("Microsoft.AspNetCore.Mvc", StringComparison.OrdinalIgnoreCase)
			|| name.StartsWith("Microsoft.AspNetCore.Components", StringComparison.OrdinalIgnoreCase)
			|| name.StartsWith("Microsoft.AspNetCore.Server", StringComparison.OrdinalIgnoreCase)
			|| name.StartsWith("Microsoft.AspNetCore.Hosting", StringComparison.OrdinalIgnoreCase)
			|| name.StartsWith("Microsoft.AspNetCore.Http", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(name, "Microsoft.AspNetCore.Routing", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(name, "Microsoft.AspNetCore.StaticFiles", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(name, "Microsoft.AspNetCore.Identity", StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsLikelyWebProject(
		IReadOnlyList<string> references,
		bool usingWebSdk,
		bool hasDesktopPlatformOutput)
	{
		if (hasDesktopPlatformOutput || HasDesktopUiStack(references))
		{
			return false;
		}

		if (usingWebSdk)
		{
			return true;
		}

		return HasAspNetWebServerReferences(references);
	}

	public static bool HasDesktopPlatformOutput(string targetPlatformIdentifier)
	{
		if (string.IsNullOrEmpty(targetPlatformIdentifier) ||
			string.Equals(targetPlatformIdentifier, "unknown", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return string.Equals(targetPlatformIdentifier, "windows", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(targetPlatformIdentifier, "macos", StringComparison.OrdinalIgnoreCase);
	}

	private static bool Contains(IReadOnlyList<string> references, string name)
	{
		if (references == null)
		{
			return false;
		}

		for (var i = 0; i < references.Count; i++)
		{
			if (string.Equals(references[i], name, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	#endregion
}
