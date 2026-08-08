#region References

using System;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Agent.Tools;

public class DateTimeTool : AgentTool
{
	#region Constructors

	public DateTimeTool(Keystone.State.AppSettings settings) : base(settings)
	{
	}

	#endregion

	#region Properties

	public override string Description => "Gets the current date, time, and timezone information.";

	public override string Name => "DateTime";

	public override string ParametersJsonSchema =>
		"""
		{
			"type": "object",
				"properties": {
				"timeZoneId": { 
					"type": "string", 
					"description": "Optional: Timezone ID (e.g. 'Eastern Standard Time', 'India Standard Time')" 
				}
			}
		}
		""";

	#endregion

	#region Methods

	public override Task<ToolResult> ExecuteAsync(PartialUpdate properties, CancellationToken ct)
	{
		var localTime = DateTime.Now;
		var utcTime = DateTime.UtcNow;

		if (properties.TryGet<string>("timeZoneId", out var tzId))
		{
			try
			{
				var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
				var tzTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
				return Task.FromResult(ToolResult.AsSuccess($"Time in timezone '{tzId}': {tzTime:yyyy-MM-dd HH:mm:ss} (Offset: {tz.BaseUtcOffset})"));
			}
			catch (Exception ex)
			{
				return Task.FromResult(ToolResult.AsError($"Timezone error: {ex.Message}"));
			}
		}

		return Task.FromResult(ToolResult.AsSuccess($"Local Time: {localTime:yyyy-MM-dd HH:mm:ss} (Timezone: {TimeZoneInfo.Local.DisplayName})\nUTC Time: {utcTime:yyyy-MM-dd HH:mm:ss}"));
	}

	#endregion
}