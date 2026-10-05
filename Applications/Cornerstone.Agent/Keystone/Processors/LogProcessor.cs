#region References

using Cornerstone.Agent.Keystone.Channels;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Keystone.Processors;

[SourceReflection]
[DependencyInjected]
public partial class LogProcessor : AppProcessor
{
	#region Constructors

	[DependencyInjectionConstructor]
	public LogProcessor(AppBus bus, AppState state) : base(bus, state)
	{
	}

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		Bus.Logging.SubscribeToLog(OnLog);
		base.InitializeLifecycle();
	}

	public override void UninitializeLifecycle()
	{
		Bus.Logging.UnsubscribeToLog(OnLog);
		base.UninitializeLifecycle();
	}

	private void OnLog(LoggingChannel.LoggingMessage message)
	{
		State.Logs.Write(message.Level, message.Message);
	}

	#endregion
}