#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Cornerstone.Text.Parsing.VisualStudio.Solution.Classic;
using NuGet.Frameworks;

#endregion

namespace Cornerstone.Text.Parsing.VisualStudio.Project;

/// <summary>
/// Represents a dot net project (new sdk)
/// </summary>
public class DotNetProject
{
	#region Fields

	private XDocument _document;
	private IEnumerable<DotNetProjectType> _projectTypeGuids;
	private XElement _root;
	private Encoding _saveEncoding;
	private IList<NuGetFramework> _targetFrameworks;
	private Version _version;
	private string _versionString;

	#endregion

	#region Properties

	public string FilePath { get; private set; }

	public IEnumerable<ItemGroup> ItemGroups => _root?.Elements().Where(x => x.Name.LocalName == "ItemGroup").Select(x => new ItemGroup(x)).ToList() ?? [];

	public IEnumerable<DotNetProjectType> ProjectTypes => _projectTypeGuids ??= ReadProjectTypeGuids();

	/// <summary>
	/// The frameworks this project targets.
	/// </summary>
	public IList<NuGetFramework> TargetFrameworks
	{
		get => _targetFrameworks ??= ReadTargetFrameworks();
		set => _targetFrameworks = value.ToList();
	}

	public Dictionary<string, string> Variables { get; private set; }

	public Version Version => _version ??= PackageReference.ParseVersionOrDefault(VersionString);

	public string VersionString => _versionString ??= ReadProjectVersion();

	#endregion

	#region Methods

	public static DotNetProject FromFile(string projectPath)
	{
		var response = new DotNetProject();
		response.LoadFile(projectPath);
		response.FilePath = Path.GetFullPath(projectPath);
		return response;
	}

	public static DotNetProject FromXml(string xml)
	{
		var response = new DotNetProject();
		response.LoadXml(xml);
		return response;
	}

	public Dictionary<string, string> GetVariables()
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		if (_root == null)
		{
			return result;
		}

		foreach (var pg in _root.Elements().Where(e => e.Name.LocalName == "PropertyGroup"))
		{
			foreach (var elem in pg.Elements())
			{
				var name = elem.Name.LocalName;
				var value = elem.Value.Trim();

				if (!string.IsNullOrWhiteSpace(value))
				{
					result[$"$({name})"] = value;
				}
			}
		}

		return result;
	}

	public void LoadFile(string projectPath)
	{
		var bytes = File.ReadAllBytes(projectPath);
		var encoding = DetectEncoding(bytes);
		using (var stream = new MemoryStream(bytes))
		{
			LoadXml(XDocument.Load(stream, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo));
		}

		_saveEncoding = encoding;
		FilePath = Path.GetFullPath(projectPath);
	}

	public void LoadXml(string xml)
	{
		Reset();
		_document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
		_root = _document.Root;
		_saveEncoding = new UTF8Encoding(false);
		Variables = GetVariables();
	}

	public void LoadXml(XDocument doc)
	{
		Reset();
		_document = doc;
		_root = doc.Root;
		_saveEncoding = new UTF8Encoding(false);
		Variables = GetVariables();
	}

	public void SaveFile(string filePath)
	{
		if ((_document == null) || string.IsNullOrEmpty(filePath))
		{
			return;
		}

		var encoding = _saveEncoding ?? new UTF8Encoding(false);
		var settings = new XmlWriterSettings
		{
			CloseOutput = false,
			Encoding = encoding,
			Indent = false,
			NewLineHandling = NewLineHandling.None,
			OmitXmlDeclaration = _document.Declaration == null
		};

		using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
		using (var writer = XmlWriter.Create(stream, settings))
		{
			_document.Save(writer);
		}
	}

	public string ToXml()
	{
		if (_root == null)
		{
			return string.Empty;
		}

		var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), _root);
		return doc.ToString();
	}

	private static Encoding DetectEncoding(byte[] bytes)
	{
		if ((bytes.Length >= 3) && (bytes[0] == 0xEF) && (bytes[1] == 0xBB) && (bytes[2] == 0xBF))
		{
			return new UTF8Encoding(true);
		}

		if ((bytes.Length >= 2) && (bytes[0] == 0xFF) && (bytes[1] == 0xFE))
		{
			return Encoding.Unicode;
		}

		if ((bytes.Length >= 2) && (bytes[0] == 0xFE) && (bytes[1] == 0xFF))
		{
			return Encoding.BigEndianUnicode;
		}

		return new UTF8Encoding(false);
	}

	private IEnumerable<DotNetProjectType> ReadProjectTypeGuids()
	{
		var projectTypeGuids = _root?
			.Elements()
			.Where(e => e.Name.LocalName == "PropertyGroup")
			.Select(e => e.Element("ProjectTypeGuids"))
			.FirstOrDefault();

		if (projectTypeGuids?.Value == null)
		{
			return [];
		}

		var guids = projectTypeGuids.Value.Split([";"], StringSplitOptions.RemoveEmptyEntries);
		return guids.Select(x => ProjectTypeIds.ToEnum(Guid.Parse(x))).ToList();
	}

	private string ReadProjectVersion()
	{
		//	<AssemblyVersion>12.0.0.0</AssemblyVersion>
		//	<FileVersion>12.0.0.0</FileVersion>
		//	<Version>12.0.0</Version>
		//
		// todo: is it worth supporting AssemblyInfo.cs?
		//

		var versions = _root?
			.Elements()
			.Where(e => e.Name.LocalName == "PropertyGroup")
			.SelectMany(e => e.Elements()
				.Where(x => x.Name.LocalName 
					is "Version"
					or "FileVersion"
					or "AssemblyVersion")
			)
			.ToList() ?? [];

		var version = versions.FirstOrDefault(x => (x.Name.LocalName == "Version"))
			?? versions.FirstOrDefault(x => (x.Name.LocalName == "FileVersion"))
			?? versions.FirstOrDefault(x => (x.Name.LocalName == "AssemblyVersion"));

		return version?.Value;
	}

	private IList<NuGetFramework> ReadTargetFrameworks()
	{
		// Special cases for Xamarin (these are still valid monikers in NuGet.Frameworks)
		if (ProjectTypes.Contains(DotNetProjectType.XamarinAndroid) ||
			ProjectTypes.Contains(DotNetProjectType.XamarinAndroidBinding))
		{
			return [NuGetFramework.Parse("monoandroid")]; // or "net8.0-android" in newer .NET
		}

		if (ProjectTypes.Contains(DotNetProjectType.XamarinIos) ||
			ProjectTypes.Contains(DotNetProjectType.XamarinIosBinding))
		{
			return [NuGetFramework.Parse("xamarinios")]; // or "net8.0-ios" in newer .NET
		}

		// Find all relevant property elements (SDK-style + classic + UWP)
		var frameworkProperties = _root?
			.Elements()
			.Where(e => e.Name.LocalName == "PropertyGroup")
			.SelectMany(pg => pg.Elements()
				.Where(e => e.Name.LocalName is "TargetFramework"
						or "TargetFrameworks"
						or "TargetFrameworkVersion" // classic .NET Framework
						or "TargetPlatformIdentifier" // UWP / platform-specific
				))
			.ToList() ?? [];

		if (!frameworkProperties.Any())
		{
			return Array.Empty<NuGetFramework>();
		}

		var monikers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var prop in frameworkProperties)
		{
			var value = prop.Value.Trim();

			if (string.IsNullOrWhiteSpace(value) || (value == "$(TargetFrameworks)"))
			{
				continue;
			}

			// Handle classic TargetFrameworkVersion (v4.8, .NETFramework,Version=v4.8)
			if ((prop.Name.LocalName == "TargetFrameworkVersion")
				&& value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
			{
				// NuGetFramework.Parse handles "v4.8", net48 automatically
				monikers.Add(value);
				continue;
			}

			// Handle UWP / platform identifier (e.g. "UAP", "Windows", "WindowsPhoneApp")
			if (prop.Name.LocalName == "TargetPlatformIdentifier")
			{
				// Usually combined with TargetPlatformVersion/MinVersion
				// For simplicity, map common cases
				var platformMoniker = value switch
				{
					"UAP" or "Windows" => "uap10.0", // most common UWP
					"WindowsPhoneApp" => "wp81", // old Windows Phone 8.1
					_ => value.ToLowerInvariant()
				};

				monikers.Add(platformMoniker);
				continue;
			}

			// Normal cases: split on semicolon (multi-targeting)
			var parts = value.Split([';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			foreach (var part in parts)
			{
				if (!string.IsNullOrWhiteSpace(part))
				{
					monikers.Add(part);
				}
			}
		}

		// Parse into NuGetFramework objects (very tolerant + future-proof)
		var frameworks = monikers
			.Select(m =>
			{
				try
				{
					return NuGetFramework.Parse(m);
				}
				catch
				{
					// Optional: log invalid moniker
					return null;
				}
			})
			.Where(f => (f != null) && !f.IsUnsupported)
			.Distinct(NuGetFrameworkFullComparer.Instance)
			.ToList();

		return frameworks;
	}

	private void Reset()
	{
		_document = null;
		_root = null;
		_projectTypeGuids = null;
		_saveEncoding = null;
		_targetFrameworks = null;
		_version = null;
		_versionString = null;
		Variables = null;
	}

	#endregion
}