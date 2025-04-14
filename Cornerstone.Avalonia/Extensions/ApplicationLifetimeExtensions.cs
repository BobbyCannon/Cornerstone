#region References

using System;
using Avalonia.Controls.ApplicationLifetimes;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Extensions;

public static class ApplicationLifetimeExtensions
{
	#region Fields

	private static string[] _args;

	#endregion

	#region Methods

	public static string[] GetSingleViewArgs(this ISingleViewApplicationLifetime lifetime)
	{
		if (_args == null)
		{
			return [];
		}

		foreach (var arg in _args)
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
		_args = args;
	}

	#endregion
}