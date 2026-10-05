namespace Cornerstone.Agent.Tools;

public class ToolResult
{
	#region Properties

	public string Output { get; set; }
	public bool Success { get; set; }

	#endregion

	#region Methods

	public static ToolResult AsError(string error)
	{
		return new ToolResult
		{
			Output = error,
			Success = false
		};
	}

	public static ToolResult AsSuccess(string output)
	{
		return new ToolResult
		{
			Output = output,
			Success = true
		};
	}

	#endregion
}