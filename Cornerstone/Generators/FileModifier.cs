#region References

using System;
using System.IO;
using System.Linq;

#endregion

namespace Cornerstone.Generators;

public static class FileModifier
{
	#region Methods

	/// <summary>
	/// </summary>
	/// <param name="startToken"> </param>
	/// <param name="endToken"> </param>
	/// <param name="filePath"> </param>
	/// <param name="code"> </param>
	/// <param name="indent"> </param>
	/// <exception cref="InvalidDataException"> </exception>
	/// <exception cref="Exception"> </exception>
	public static void UpdateFileIfNecessary(string startToken, string endToken, string filePath, string code, string indent = "\t\t\t")
	{
		if (!File.Exists(filePath))
		{
			throw new InvalidDataException("File path is incorrect...");
		}

		var fileData = File.ReadAllText(filePath);
		var startIndex = fileData.IndexOf(startToken);
		if (startIndex == -1)
		{
			throw new InvalidDataException("Failed to find start token...");
		}

		startIndex += startToken.Length;
		var endIndex = fileData.IndexOf(endToken, startIndex);
		if ((endIndex == -1) || (endIndex <= startIndex))
		{
			throw new InvalidDataException("Failed to find end token...");
		}

		var currentScenarios = fileData.Substring(startIndex, endIndex - startIndex);

		// Remove any white spaces
		var newScenarios = code.Trim();

		// Remove last ',' comma.
		if (code.EndsWith(","))
		{
			newScenarios = newScenarios.Substring(0, newScenarios.Length - 1);
		}

		// Add indentation to new scenarios
		newScenarios = string.Join(Environment.NewLine,
			newScenarios
				.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
				.Select(x => $"{indent}{x}")
		);
		newScenarios += Environment.NewLine + indent;

		if (string.Equals(currentScenarios, newScenarios))
		{
			// nothing needs to change
			return;
		}

		// Remove old tests
		fileData = fileData.Remove(startIndex, endIndex - startIndex);
		fileData = fileData.Insert(startIndex, newScenarios);

		// Save the file
		File.WriteAllText(filePath, fileData);
	}

	#endregion
}