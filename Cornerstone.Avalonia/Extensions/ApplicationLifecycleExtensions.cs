#region References

using System;
using Avalonia.Controls.ApplicationLifetimes;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Extensions;

public static class ApplicationLifecycleExtensions
{
	#region Fields

	private static string[] _browserArgs;

	#endregion

	#region Methods

	public static string[] GetBrowserArgs(this ISingleViewApplicationLifetime lifetime)
	{
		if (_browserArgs == null)
		{
			return [];
		}

		foreach (var arg in _browserArgs)
		{
			if (Uri.TryCreate(arg, UriKind.RelativeOrAbsolute, out var uri))
			{
				return uri.ToApplicationArguments();
			}
		}

		return [];
	}

	public static void SetBrowserArgs(string[] args)
	{
		_browserArgs = args;
	}

	#endregion
}