#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Cornerstone.Parsers.VisualStudio.Solution;
using XmlDeclaration = Cornerstone.Parsers.Xml.XmlDeclaration;
using XmlDocument = Cornerstone.Parsers.Xml.XmlDocument;
using XmlElement = Cornerstone.Parsers.Xml.XmlElement;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Project;

/// <summary>
/// Represents a dot net project (new sdk)
/// </summary>
public class DotNetProject : XmlDocument
{
	#region Fields

	private IEnumerable<DotNetProjectType> _projectTypeGuids;
	private IList<TargetFramework> _targetFrameworks;
	private Version _version;
	private string _versionString;

	#endregion

	#region Constructors

	/// <inheritdoc />
	internal DotNetProject() : base("Project")
	{
	}

	#endregion

	#region Properties

	public IEnumerable<ItemGroup> ItemGroups => Elements.Where(x => x is ItemGroup).Cast<ItemGroup>().ToList();

	public IEnumerable<DotNetProjectType> ProjectTypes => _projectTypeGuids ??= ReadProjectTypeGuids();

	/// <summary>
	/// The frameworks this project targets.
	/// </summary>
	public IEnumerable<TargetFramework> TargetFrameworks
	{
		get => _targetFrameworks ??= ReadTargetFrameworks();
		set => _targetFrameworks = value.ToList();
	}

	public Version Version => _version ??= ParseVersionOrDefault(VersionString);

	public string VersionString => _versionString ??= ReadProjectVersion();

	#endregion

	#region Methods

	public static DotNetProject FromFile(string projectPath)
	{
		var response = new DotNetProject();
		response.LoadFile(projectPath);
		return response;
	}

	public static DotNetProject FromXml(string xml)
	{
		var stream = new StringReader(xml);
		using var reader = new XmlTextReader(stream);
		return FromXml(reader);
	}

	public static DotNetProject FromXml(XmlTextReader reader)
	{
		var response = new DotNetProject();
		response.LoadXml(reader);
		return response;
	}

	public override void LoadXml(XmlTextReader reader)
	{
		Reset();

		reader.Read();

		while ((reader.Name != "Project") && !reader.EOF)
		{
			if (reader.NodeType == XmlNodeType.XmlDeclaration)
			{
				var element = new XmlElement("Xml");
				ReadAttributes(reader, element);
				XmlDeclaration = XmlDeclaration.FromAttributes(element.Attributes);
			}
			else
			{
				reader.Read();
			}
		}

		if (reader.EOF)
		{
			throw new ParserException("The file does not contain the [Project] element.", reader.LineNumber, reader.LinePosition);
		}

		ProcessReader(reader, this);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		var name = ElementName;
		base.Reset();
		ElementName = name;
	}

	protected override XmlElement CreateElement(XmlReader reader)
	{
		return reader.Name switch
		{
			"ItemGroup" => new ItemGroup(),
			"PackageReference" => new PackageReference(),
			"PropertyGroup" => new PropertyGroup(),
			"Reference" => new ClassicReference(),
			_ => new XmlElement(reader.Name)
		};
	}

	private IEnumerable<DotNetProjectType> ReadProjectTypeGuids()
	{
		var projectTypeGuids = Elements
			.Where(x => x is PropertyGroup)
			.Select(e => e.Elements.FirstOrDefault(x => x.ElementName is "ProjectTypeGuids"))
			.FirstOrDefault();

		if (projectTypeGuids?.ElementValue == null)
		{
			return [];
		}

		var guids = projectTypeGuids.ElementValue.Split([";"], StringSplitOptions.RemoveEmptyEntries);
		return guids.Select(x => ProjectTypeIds.ToEnum(Guid.Parse(x))).ToList();
	}

	private string ReadProjectVersion()
	{
		//	<AssemblyVersion>12.0.4.0</AssemblyVersion>
		//	<FileVersion>12.0.4.0</FileVersion>
		//	<Version>12.0.4</Version>
		//
		// todo: is it worth supporting AssemblyInfo.cs?
		//

		var versions = Elements
			.Where(x => x is PropertyGroup)
			.SelectMany(e => e.Elements
				.Where(x => x.ElementName is "Version"
					or "FileVersion"
					or "AssemblyVersion")
			)
			.ToList();

		var version = versions.FirstOrDefault(x => (x.ElementName == "Version") && (x.ElementValue != null))
			?? versions.FirstOrDefault(x => (x.ElementName == "FileVersion") && (x.ElementValue != null))
			?? versions.FirstOrDefault(x => (x.ElementName == "AssemblyVersion") && (x.ElementValue != null));

		return version?.ElementValue;
	}

	private IList<TargetFramework> ReadTargetFrameworks()
	{
		if (ProjectTypes.Contains(DotNetProjectType.XamarinAndroid)
			|| ProjectTypes.Contains(DotNetProjectType.XamarinAndroidBinding))
		{
			return [TargetFrameworkService.GetByType(TargetFrameworkType.MonoAndroid)];
		}

		if (ProjectTypes.Contains(DotNetProjectType.XamarinIos)
			|| ProjectTypes.Contains(DotNetProjectType.XamarinIosBinding))
		{
			return [TargetFrameworkService.GetByType(TargetFrameworkType.XamarinIos)];
		}

		var frameworks = Elements
			.Where(x => x is PropertyGroup)
			.SelectMany(e =>
				e.Elements
					.Where(x =>
						// Latest project format
						x.ElementName is "TargetFramework"
							or "TargetFrameworks"
							// Classic Project
							or "TargetFrameworkVersion"
							// UAP (UWP)
							or "TargetPlatformIdentifier"
					)
					.ToList()
			)
			.ToList();

		if (frameworks.Count <= 0)
		{
			return Array.Empty<TargetFramework>();
		}

		var monikers = frameworks
			.SelectMany(x => x.ElementValue.Split([";"], StringSplitOptions.RemoveEmptyEntries))
			// Exclude these values
			.Where(x => x != "$(TargetFrameworks)")
			.ToList();

		return monikers
			.Select(TargetFrameworkService.GetOrAddFramework)
			.ToList();
	}

	#endregion
}