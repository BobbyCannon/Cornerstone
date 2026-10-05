#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Presentation.Documentation;

/// <summary>
/// Optional <c>appsettings.json</c> root (standard .NET JSON next to the EXE, not Cornerstone SettingsFile).
/// </summary>
public sealed class DocumentationAppSettings
{
	#region Properties

	public DocumentationAppSettingsSection Documentation { get; set; }

	#endregion
}

/// <summary>
/// <c>Documentation</c> section of <c>appsettings.json</c>.
/// </summary>
public sealed class DocumentationAppSettingsSection
{
	#region Properties

	public List<string> ExportIncludePaths { get; set; }

	#endregion
}
