namespace Cornerstone.Keystone.Messages;

public class ChannelMessage : IChannelMessage
{
	#region Constructors

	public ChannelMessage()
	{
	}

	public ChannelMessage(object value)
	{
		Payload = value;
	}

	#endregion

	#region Properties

	public object Payload { get; set; }

	#endregion
}

public interface IChannelMessage
{
}