#region References

using System;
using Cornerstone.Parsers.Xml;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Project;

public class PackageReference : XmlElement
{
	#region Fields

	private Version _version;
	private string _versionString;

	#endregion

	#region Constructors

	/// <inheritdoc />
	public PackageReference() : base("PackageReference")
	{
	}

	#endregion

	#region Properties

	public string Include => GetAttributeValue(nameof(Include));

	public Version Version => _version ??= ParseVersionOrDefault(VersionString);

	public string VersionString => _versionString ??= GetAttributeOrElementValue(nameof(Version));

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

	public bool UpdateVersion(string versionString)
	{
		return SetAttributeOrElementValue(nameof(Version), versionString);
	}

	#endregion
}