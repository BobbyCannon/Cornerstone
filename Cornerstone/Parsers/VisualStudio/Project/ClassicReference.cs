#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Parsers.Xml;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Project;

/// <summary>
/// </summary>
public class ClassicReference : XmlElement
{
	#region Fields

	private IDictionary<string, string> _includeDictionary;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public ClassicReference() : base("Reference")
	{
		// Just for reference
		//	<Reference Include="Speedy, Version=12.0.1.0, Culture=neutral, PublicKeyToken=8db7b042d9663bf8, processorArchitecture=MSIL">
		//		<HintPath>..\packages\Speedy.12.0.1\lib\netstandard2.0\Speedy.dll</HintPath>
		//	</Reference>
	}

	#endregion

	#region Properties

	public string HintPath => GetElementValue(nameof(HintPath));

	public string Include => GetAttributeValue(nameof(Include));

	public string IncludeName => GetIncludeLookup().TryGetValue("Name", out var value) ? value : string.Empty;

	public string IncludeVersion => GetIncludeLookup().TryGetValue("Version", out var value) ? value : string.Empty;

	#endregion

	#region Methods

	public void Update(string version, string hintPath)
	{
		if (!string.IsNullOrWhiteSpace(IncludeVersion))
		{
			TrySetAttributeValue(nameof(Include), Include.Replace(IncludeVersion, version));
		}

		TrySetElementValue(nameof(HintPath), hintPath);

		_includeDictionary = null;
	}

	private IDictionary<string, string> GetIncludeLookup()
	{
		if (Include == null)
		{
			return new Dictionary<string, string>();
		}

		_includeDictionary ??= Include
			.Split(',')
			.Select(x => x.Split('='))
			.Where(x => x.Length >= 1)
			.ToDictionary(x => x.Length > 1 ? x[0].Trim() : "Name", x => x.Length > 1 ? x[1] : x[0].Trim());
		return _includeDictionary;
	}

	#endregion
}