#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Project;

public class ClassicReference
{
	#region Fields

	private readonly XElement _element;
	private IDictionary<string, string> _includeDictionary;

	#endregion

	#region Constructors

	public ClassicReference(XElement element)
	{
		_element = element ?? throw new ArgumentNullException(nameof(element));
	}

	#endregion

	#region Properties

	public string HintPath => _element.Element("HintPath")?.Value;

	public string Include => _element.Attribute("Include")?.Value;

	public string IncludeName => GetIncludeLookup().TryGetValue("Name", out var value) ? value : string.Empty;

	public string IncludeVersion => GetIncludeLookup().TryGetValue("Version", out var value) ? value : string.Empty;

	#endregion

	#region Methods

	public void Update(string version, string hintPath)
	{
		if (!string.IsNullOrWhiteSpace(IncludeVersion))
		{
			var newInclude = Include?.Replace(IncludeVersion, version);
			_element.SetAttributeValue("Include", newInclude);
		}

		var hintPathElement = _element.Element("HintPath");
		if (hintPathElement != null)
		{
			hintPathElement.SetValue(hintPath);
		}
		else
		{
			_element.AddFirst(new XElement("HintPath", hintPath));
		}

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