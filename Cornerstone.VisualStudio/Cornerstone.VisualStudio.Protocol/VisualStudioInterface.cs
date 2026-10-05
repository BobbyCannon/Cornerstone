#region References

using System.Threading.Tasks;

#endregion

namespace Cornerstone.VisualStudio.Protocol;

/// <summary>
/// IDE-facing snapshot. Visual Studio fills Text and Caret; the methods forward that state to the editor process.
/// </summary>
public sealed class VisualStudioInterface
{
	#region Fields

	private readonly EditorConnection _connection;

	#endregion

	#region Constructors

	public VisualStudioInterface(EditorConnection connection)
	{
		_connection = connection;
		Text = string.Empty;
	}

	#endregion

	#region Properties

	public int Caret { get; set; }

	public string Text { get; set; }

	#endregion

	#region Methods

	public Task<GetCompletionsResponseMessage> GetCompletionsAsync()
	{
		return _connection.GetCompletionsAsync(Text, Caret);
	}

	public Task<BuildEnterIndentResponseMessage> BuildEnterIndentAsync(
		string line,
		int caretIndex,
		string previousLine,
		int indentSize,
		string newLine)
	{
		return _connection.BuildEnterIndentAsync(line, caretIndex, previousLine, indentSize, newLine);
	}

	#endregion
}