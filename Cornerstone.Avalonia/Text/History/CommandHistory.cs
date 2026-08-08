#region References

using System;
using Cornerstone.Data;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Avalonia.Text.History;

[Notifiable(["*"])]
public partial class CommandHistory : CornerstoneObject
{
	#region Constructors

	public CommandHistory(string command)
	{
		Command = command;
		CreatedOn = DateTimeProvider.RealTime.UtcNow;
		Count = 1;
	}

	#endregion

	#region Properties

	public partial string Command { get; set; }

	public partial int Count { get; set; }

	public partial DateTime CreatedOn { get; set; }

	#endregion
}