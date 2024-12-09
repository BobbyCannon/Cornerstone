#region References

using System;
using System.Diagnostics.Tracing;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Logging;

public class LogEventView : Bindable<LogEventView>
{
	#region Properties

	/// <summary>
	/// The level of the event.
	/// </summary>
	public EventLevel Level { get; set; }

	/// <summary>
	/// The message for the event.
	/// </summary>
	public string Message { get; set; }

	/// <summary>
	/// The date time of the event
	/// </summary>
	public DateTime MessagedOn { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Update the LogEventView with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The settings for controlling the updating of the entity. </param>
	public override bool UpdateWith(LogEventView update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			Level = update.Level;
			Message = update.Message;
			MessagedOn = update.MessagedOn;
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Level)), x => x.Level = update.Level);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Message)), x => x.Message = update.Message);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(MessagedOn)), x => x.MessagedOn = update.MessagedOn);
		}

		return true;
	}

	/// <inheritdoc />
	public override bool UpdateWith(object update, IncludeExcludeSettings settings)
	{
		return update switch
		{
			LogEventView value => UpdateWith(value, settings),
			_ => base.UpdateWith(update, settings)
		};
	}

	#endregion
}