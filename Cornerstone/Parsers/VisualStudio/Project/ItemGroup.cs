using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NuGet.Frameworks;
using System.Xml.Linq;

namespace Cornerstone.Parsers.VisualStudio.Project;

public class ItemGroup
{
	#region Fields

	private readonly Lazy<IReadOnlyList<NuGetFramework>> lazyTargetFrameworks;

	private static readonly Regex ConditionRegex = new(
		@"'\$\(TargetFramework\)'\s*==\s*'(?<framework>[^']+)'",
		RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
	);

	private readonly XElement _element;

	#endregion

	#region Constructors

	public ItemGroup(XElement element)
	{
		_element = element ?? throw new ArgumentNullException(nameof(element));
		lazyTargetFrameworks = new Lazy<IReadOnlyList<NuGetFramework>>(ComputeTargetFrameworks);
	}

	#endregion

	#region Properties

	public IReadOnlyList<PackageReference> PackageReferences =>
		_element.Elements()
			.Where(e => e.Name.LocalName == "PackageReference")
			.Select(e => new PackageReference(e))
			.ToList();

	public IReadOnlyList<ClassicReference> References =>
		_element.Elements()
			.Where(e => e.Name.LocalName == "Reference")
			.Select(e => new ClassicReference(e))
			.ToList();

	/// <summary>
	/// Returns the list of target frameworks this ItemGroup is conditionally applied to.
	/// Empty list if the ItemGroup is unconditional or condition doesn't match known pattern.
	/// </summary>
	public IReadOnlyList<NuGetFramework> TargetFrameworks => lazyTargetFrameworks.Value;

	#endregion

	#region Methods

	private IReadOnlyList<NuGetFramework> ComputeTargetFrameworks()
	{
		var conditionAttr = _element.Attribute("Condition");

		if (conditionAttr?.Value is not { Length: > 0 } condition)
		{
			return Array.Empty<NuGetFramework>();
		}

		var matches = ConditionRegex.Matches(condition);

		if (matches.Count == 0)
		{
			return Array.Empty<NuGetFramework>();
		}

		var frameworks = new List<NuGetFramework>(matches.Count);

		foreach (Match match in matches)
		{
			var frameworkName = match.Groups["framework"].Value.Trim();

			if (string.IsNullOrWhiteSpace(frameworkName))
			{
				continue;
			}

			try
			{
				var fw = NuGetFramework.Parse(frameworkName);
				if (!fw.IsUnsupported)
				{
					frameworks.Add(fw);
				}
			}
			catch
			{
				// Invalid/unknown moniker → skip silently
				// You could log here if needed
			}
		}

		// Remove duplicates (rare but possible in malformed conditions)
		return frameworks
			.Distinct(NuGetFrameworkFullComparer.Instance)
			.ToList();
	}

	#endregion
}
