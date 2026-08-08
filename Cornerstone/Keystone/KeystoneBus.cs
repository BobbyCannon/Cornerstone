#region References

using System;
using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Keystone.Messages;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Keystone;

[SourceReflection]
public class KeystoneBus : LifecycleTracker
{
	#region Constructors

	[DependencyInjectionConstructor]
	public KeystoneBus()
	{
		History = new PresentationList<ChannelMessageHistory> { Limit = 100 };
	}

	#endregion

	#region Properties

	public PresentationList<ChannelMessageHistory> History { get; }

	public Action<Exception> OnError { get; set; }

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		foreach (var child in Children)
		{
			if (child is KeystoneChannel channel)
			{
				channel.ErrorOccurred += OnError;
				channel.MessagePublished += OnMessagePublished;
			}
		}

		base.InitializeLifecycle();
	}

	public override void UninitializeLifecycle()
	{
		foreach (var child in Children)
		{
			if (child is KeystoneChannel channel)
			{
				channel.ErrorOccurred -= OnError;
				channel.MessagePublished -= OnMessagePublished;
			}
		}

		base.UninitializeLifecycle();
	}

	private void OnMessagePublished(int type, IChannelMessage message)
	{
		// todo: track history?
	}

	#endregion
}