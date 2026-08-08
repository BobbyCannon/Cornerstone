#region References

using System;
using System.Xml.Linq;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Project;

public class PackageReference
{
	#region Fields

	private readonly XElement _element;
	private Version _version;
	private string _versionString;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public PackageReference(XElement element)
	{
		_element = element ?? throw new ArgumentNullException(nameof(element));
	}

	#endregion

	#region Properties

	public string Include => _element.Attribute("Include")?.Value;

	public Version Version => _version ??= ParseVersionOrDefault(VersionString);

	public string VersionString => _versionString ??= GetVersionFromAttributeOrElement();

	#endregion

	#region Methods

	/// <summary>
	/// Compare the provided version to this package reference.
	/// </summary>
	/// <param name="version"> The version to compare to. </param>
	/// <returns>
	/// -1 the reference is less than provided version.
	/// 0 the reference is equal to the version.
	/// 1 the reference is larger than the version.
	/// </returns>
	public int Compare(Version version)
	{
		if (string.IsNullOrWhiteSpace(VersionString))
		{
			return -1;
		}

		if (VersionString.StartsWith("["))
		{
			return -1;
		}

		return Version?.CompareTo(version) ?? -1;
	}

	/// <summary>
	/// Parses version then normalizes to match nuget.
	/// </summary>
	/// <param name="versionString"> The version string. </param>
	/// <returns> Returns Version if found otherwise returns null. </returns>
	public static Version ParseVersionOrDefault(string versionString)
	{
		if (string.IsNullOrWhiteSpace(versionString))
		{
			return null;
		}

		versionString = versionString.Trim();

		// Remove wrapping brackets if present, treat [1.2.3] same as 1.2.3
		if (versionString.StartsWith('[') && versionString.EndsWith(']'))
		{
			versionString = versionString[1..^1].Trim();
		}
		else if (versionString.StartsWith('(') || versionString.Contains(','))
		{
			// Real range detected, for now we reject (or you could extract min version)
			return null;
		}

		// Handle wildcard style: 9.*, 9.0.*, 9.1.*
		if (versionString.Contains('*'))
		{
			var parts = versionString.Split('.');
			if (parts.Length is < 2 or > 4)
			{
				return null;
			}

			int major = 0, minor = 0, build = 0, rev = 0;

			for (var i = 0; i < parts.Length; i++)
			{
				var p = parts[i].Trim();
				if (p == "*")
				{
					// Wildcard, set to 0 and stop parsing further
					break;
				}
				if (!int.TryParse(p, out var val))
				{
					return null;
				}

				switch (i)
				{
					case 0: major = val; break;
					case 1: minor = val; break;
					case 2: build = val; break;
					case 3: rev = val; break;
				}
			}

			return new Version(major, minor, Math.Max(build, 0), Math.Max(rev, 0));
		}

		// Normal version parsing with your existing normalization
		return Version.TryParse(versionString, out var ver)
			? new Version(ver.Major, ver.Minor, Math.Max(ver.Build, 0), Math.Max(ver.Revision, 0))
			: null;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{Include}:{VersionString}";
	}

	public bool UpdateVersion(string versionString)
	{
		// XElement.SetAttributeValue handles both updating an existing attribute or creating a new one.
		// If the project file format requires an <Version> element instead of an attribute, 
		// you may need to explicitly remove the attribute and add an element, but standard SDK-style 
		// project files use attributes.
		_element.SetAttributeValue("Version", versionString);
		_versionString = null; // Invalidate cache
		return true;
	}

	private string GetVersionFromAttributeOrElement()
	{
		// Check attribute first (SDK-style)
		var attrValue = _element.Attribute("Version")?.Value;
		if (!string.IsNullOrWhiteSpace(attrValue))
		{
			return attrValue;
		}

		// Check element (classic/manual style)
		return _element.Element("Version")?.Value;
	}

	#endregion
}