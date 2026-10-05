#region References

using Microsoft.Build.Framework;

#endregion

namespace Cornerstone.Presentation.UnitTests.BuildTasks;

internal enum MessageSource
{
	Unknown,
	ErrorEvent,
	MessageEvent,
	CustomEvent,
	WarningEvent
}

internal record class UnitTestBuildEngineMessage
{
	#region Constructors

	private UnitTestBuildEngineMessage(MessageSource Type, LazyFormattedBuildEventArgs Source)
	{
		this.Type = Type;
		this.Source = Source;
		Message = Source.Message;
	}

	#endregion

	#region Properties

	public string Message { get; }
	public LazyFormattedBuildEventArgs Source { get; }

	public MessageSource Type { get; }

	#endregion

	#region Methods

	public static UnitTestBuildEngineMessage From(BuildWarningEventArgs buildWarning)
	{
		return new(MessageSource.WarningEvent, buildWarning);
	}

	public static UnitTestBuildEngineMessage From(BuildMessageEventArgs buildMessage)
	{
		return new(MessageSource.MessageEvent, buildMessage);
	}

	public static UnitTestBuildEngineMessage From(BuildErrorEventArgs buildError)
	{
		return new(MessageSource.ErrorEvent, buildError);
	}

	public static UnitTestBuildEngineMessage From(CustomBuildEventArgs customBuild)
	{
		return new(MessageSource.CustomEvent, customBuild);
	}

	#endregion
}