#region References

using System;

#endregion

namespace Cornerstone.Keystone.Messages;

public class ChannelMessageHistory
{
	#region Properties

	public string Name { get; set; }

	public DateTime PublishOn { get; set; }

	public int Type { get; set; }

	#endregion
}