#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.VisualStudio;

internal static class CornerstoneConstants
{
	#region Constants

	public const string AvaloniaCapability = nameof(Avalonia);
	public const string Axaml = nameof(Axaml);
	public const string CommandSetGuidString = "B7E6A0F1-4C2D-4A8E-9F31-0D5C8A2E7B10";
	public const string CornerstoneFactoryEditorGuidString = "6D5344A2-2FCD-49DE-A09D-6A14FD1B1224";
	public const int CodeCleanupDocumentCommandId = 0x0100;
	public const int CodeCleanupHierarchyCommandId = 0x0101;
	/// <summary>
	/// TEMP: Code Cleanup UI is hidden for the current release. Set to true (and restore
	/// menus in CornerstonePackage.vsct / OptionsView.xaml) when shipping Code Cleanup.
	/// </summary>
	public const bool CodeCleanupUiEnabled = false;
	public const string PackageGuidString = "865ba8d5-1180-4bf8-8821-345f72a4cb79";
	public const string PackageName = "Cornerstone";
	public const string Xaml = nameof(Xaml);

	#endregion

	#region Fields

	public static readonly Guid PackageGuid = new(PackageGuidString);
	public static readonly Guid CommandSetGuid = new(CommandSetGuidString);
	public static readonly Guid CornerstoneFactoryEditorGuid = new(CornerstoneFactoryEditorGuidString);

	#endregion

	#region Properties

	/// <summary>
	/// Three-part extension version (Major.Minor.Build) from the assembly stamp.
	/// </summary>
	/// <remarks>
	/// Kept in lockstep with Directory.Build.props / vsixmanifest via Update-ExtensionVersion.ps1.
	/// </remarks>
	public static string PackageVersion
	{
		get
		{
			var version = typeof(CornerstoneConstants).Assembly.GetName().Version;
			if (version is null)
			{
				return string.Empty;
			}

			return $"{version.Major}.{version.Minor}.{version.Build}";
		}
	}

	#endregion
}