#region References

using System;
using System.IO;
using System.Linq;
using System.Reflection;

#endregion

namespace Cornerstone.Automation;

public static class ResourceService
{
	#region Fields

	private static readonly Lazy<string> TestScript = new(LoadTestScript);

	#endregion

	#region Methods

	/// <summary>
	/// Inserts the test script into the current page.
	/// </summary>
	public static string GetTestScript()
	{
		return TestScript.Value;
	}

	private static string LoadTestScript()
	{
		var assembly = Assembly.GetExecutingAssembly();

		using (var stream = assembly.GetManifestResourceStream("Cornerstone.Automation.Cornerstone.Automation.js"))
		{
			if (stream == null)
			{
				return string.Empty;
			}

			using (var reader = new StreamReader(stream))
			{
				var data = reader.ReadToEnd();
				var lines = data.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
				for (var i = 0; i < lines.Length; i++)
				{
					lines[i] = lines[i].Trim();
				}

				return string.Join("", lines.Where(x => !x.StartsWith("//"))).Replace("\t", "");
			}
		}
	}

	#endregion
}