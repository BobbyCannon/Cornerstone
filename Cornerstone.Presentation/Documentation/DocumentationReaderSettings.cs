#region References

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Settings;

#endregion

namespace Cornerstone.Presentation.Documentation;

/// <summary>
/// User settings for a documentation reader host. Stored as ApplicationSettings.json
/// in the directory from <see cref="GetDirectory" />.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"], false)]
public partial class DocumentationReaderSettings : SettingsFile<DocumentationReaderSettings>
{
	#region Constructors

	/// <summary>
	/// Serialization use only.
	/// </summary>
	public DocumentationReaderSettings()
	{
	}

	public DocumentationReaderSettings(IRuntimeInformation runtimeInformation)
		: base("ApplicationSettings.json", GetDirectory(runtimeInformation))
	{
	}

	/// <summary>
	/// Loads and saves ApplicationSettings.json in <paramref name="directory" />.
	/// </summary>
	public DocumentationReaderSettings(string directory)
		: base("ApplicationSettings.json", directory)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// Article column: <see cref="DocumentationReadingWidth.Column" /> or <see cref="DocumentationReadingWidth.Full" />.
	/// </summary>
	public partial DocumentationReadingWidth ReadingWidth { get; set; }

	public partial WindowLocation WindowLocation { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Settings folder for this process. Uses <see cref="IRuntimeInformation.ApplicationDataLocation" />.
	/// When that folder is shared, the application name is appended so each host keeps its own file.
	/// </summary>
	public static string GetDirectory(IRuntimeInformation runtimeInformation)
	{
		var location = runtimeInformation?.ApplicationDataLocation;
		if (string.IsNullOrWhiteSpace(location))
		{
			return string.Empty;
		}

		var name = runtimeInformation.ApplicationName?.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			return location;
		}

		var trimmed = location.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var folder = Path.GetFileName(trimmed);
		if (string.Equals(folder, name, StringComparison.OrdinalIgnoreCase))
		{
			return location;
		}

		return Path.Combine(location, name);
	}

	public override JsonSerializerOptions GetSerializationSettings()
	{
		var options = new JsonSerializerOptions(Serializer.SerializationOptions);
		options.Converters.Add(new JsonStringEnumConverter<DocumentationReadingWidth>());
		return options;
	}

	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return base.HasChanges(settings)
			|| (WindowLocation?.HasChanges() ?? false);
	}

	public override void ResetHasChanges()
	{
		WindowLocation?.ResetHasChanges();
		base.ResetHasChanges();
	}

	protected override void FinalizeLoad()
	{
		WindowLocation ??= new WindowLocation();
		if (!Enum.IsDefined(typeof(DocumentationReadingWidth), ReadingWidth))
		{
			ReadingWidth = DocumentationReadingWidth.Column;
		}

		base.FinalizeLoad();
	}

	#endregion
}
