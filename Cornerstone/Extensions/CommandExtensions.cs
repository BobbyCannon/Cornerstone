﻿#region References

using System.Windows.Input;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for commands
/// </summary>
public static class CommandExtensions
{
	#region Methods

	/// <summary>
	/// Refresh the command state.
	/// </summary>
	public static void Refresh(this ICommand command)
	{
		if (command is RelayCommand rCommand)
		{
			rCommand.Refresh();
		}
	}

	#endregion
}