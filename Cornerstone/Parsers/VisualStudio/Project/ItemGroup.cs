#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cornerstone.Parsers.Xml;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Project;

public class ItemGroup : XmlElement
{
	#region Fields

	private static readonly Regex _conditions;
	private IList<TargetFramework> _targetFrameworks;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public ItemGroup() : base("ItemGroup")
	{
	}

	static ItemGroup()
	{
		_conditions = new(@"'\$\(TargetFramework\)' == '(?<framework>[^']+)'", RegexOptions.Compiled);
	}

	#endregion

	#region Properties

	public IEnumerable<PackageReference> PackageReferences =>
		Elements
			.Where(x => x is PackageReference)
			.Cast<PackageReference>()
			.ToList();

	public IEnumerable<ClassicReference> References =>
		Elements
			.Where(x => x is ClassicReference)
			.Cast<ClassicReference>()
			.ToList();

	public IEnumerable<TargetFramework> TargetFrameworks => _targetFrameworks ??= ReadTargetFrameworks();

	#endregion

	#region Methods

	private IList<TargetFramework> ReadTargetFrameworks()
	{
		var conditions = Attributes.FirstOrDefault(x => x.Name == "Condition")?.Value;

		if (conditions == null)
		{
			return Array.Empty<TargetFramework>();
		}

		#if (NETSTANDARD)
		var matchCollection = _conditions.Matches(conditions).Cast<Match>();
		#else
		var matchCollection = _conditions.Matches(conditions);
		#endif

		var matches = matchCollection.Select(x => x.Groups["framework"].Value);
		return matches.Select(TargetFrameworkService.GetOrAddFramework).ToList();
	}

	#endregion
}