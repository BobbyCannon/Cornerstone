#region References

using System;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution;

/// <summary>
/// Solution project type IDs.
/// </summary>
/// <remarks>
/// https://github.com/JamesW75/visual-studio-project-type-guid
/// </remarks>
public static class ProjectTypeIds
{
	#region Constants

	public const string AspNet5 = "8BB2217D-0F2D-49D1-97BC-3654ED321F3B";
	public const string AspNetMvc1 = "603C0E0B-DB56-11DC-BE95-000D561079B0";
	public const string AspNetMvc2 = "F85E285D-A4E0-4152-9332-AB1D724D3325";
	public const string AspNetMvc3 = "E53F8FEA-EAE0-44A6-8774-FFD645390401";
	public const string AspNetMvc4 = "E3E379DF-F4C6-4180-9B81-6769533ABE47";
	public const string AspNetMvc5 = "349C5851-65DF-11DA-9384-00065B846F21";
	public const string CPlusPlus = "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942";
	public const string CSharp = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
	public const string CSharpDotNetCore = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";
	public const string Database = "A9ACE9BB-CECE-4E62-9AA4-C7E7C5BD2124";
	public const string DatabaseOther = "4F174C21-8C12-11D0-8340-0000F80270F8";
	public const string SolutionFolder = "2150E333-8FDC-42A3-9474-1A3956D46DE8";
	public const string UniversalWindowsClassLibrary = "A5A43C5B-DE2A-4C0C-9213-0A381AF9435A";
	public const string XamarinAndroid = "EFBA0AD7-5A72-4C68-AF49-83D382785DCF";
	public const string XamarinAndroidBinding = "10368E6C-D01B-4462-8E8B-01FC667A7035";
	public const string XamarinIos = "6BC8ED88-2882-458C-8E55-DFD12B67127B";
	public const string XamarinIos2 = "FEACFBD2-3405-455C-9665-78FE426C6842";
	public const string XamarinIosBinding = "8FFB629D-F513-41CE-95D2-7ECE97B6EEEC";

	#endregion

	#region Methods

	public static string ToCodeString(Guid guid)
	{
		var id = guid.ToString().ToUpper();
		return id switch
		{
			AspNet5 => $"{nameof(ProjectTypeIds)}.{nameof(AspNet5)}",
			AspNetMvc1 => $"{nameof(ProjectTypeIds)}.{nameof(AspNetMvc1)}",
			AspNetMvc2 => $"{nameof(ProjectTypeIds)}.{nameof(AspNetMvc2)}",
			AspNetMvc3 => $"{nameof(ProjectTypeIds)}.{nameof(AspNetMvc3)}",
			AspNetMvc4 => $"{nameof(ProjectTypeIds)}.{nameof(AspNetMvc4)}",
			AspNetMvc5 => $"{nameof(ProjectTypeIds)}.{nameof(AspNetMvc5)}",
			CPlusPlus => $"{nameof(ProjectTypeIds)}.{nameof(CPlusPlus)}",
			CSharp => $"{nameof(ProjectTypeIds)}.{nameof(CSharp)}",
			CSharpDotNetCore => $"{nameof(ProjectTypeIds)}.{nameof(CSharpDotNetCore)}",
			Database => $"{nameof(ProjectTypeIds)}.{nameof(Database)}",
			DatabaseOther => $"{nameof(ProjectTypeIds)}.{nameof(DatabaseOther)}",
			SolutionFolder => $"{nameof(ProjectTypeIds)}.{nameof(SolutionFolder)}",
			UniversalWindowsClassLibrary => $"{nameof(ProjectTypeIds)}.{nameof(UniversalWindowsClassLibrary)}",
			XamarinAndroid => $"{nameof(ProjectTypeIds)}.{nameof(XamarinAndroid)}",
			XamarinAndroidBinding => $"{nameof(ProjectTypeIds)}.{nameof(XamarinAndroidBinding)}",
			XamarinIos => $"{nameof(ProjectTypeIds)}.{nameof(XamarinIos)}",
			XamarinIos2 => $"{nameof(ProjectTypeIds)}.{nameof(XamarinIos)}",
			XamarinIosBinding => $"{nameof(ProjectTypeIds)}.{nameof(XamarinIosBinding)}",
			_ => $"new Guid(\"{id.ToUpper()}\")"
		};
	}

	public static DotNetProjectType ToEnum(Guid projectTypeId)
	{
		var id = projectTypeId.ToString().ToUpper();
		return id switch
		{
			AspNet5 => DotNetProjectType.AspNet5,
			AspNetMvc1 => DotNetProjectType.AspNetMvc1,
			AspNetMvc2 => DotNetProjectType.AspNetMvc2,
			AspNetMvc3 => DotNetProjectType.AspNetMvc3,
			AspNetMvc4 => DotNetProjectType.AspNetMvc4,
			AspNetMvc5 => DotNetProjectType.AspNetMvc5,
			CPlusPlus => DotNetProjectType.CPlusPlus,
			CSharp => DotNetProjectType.CSharp,
			CSharpDotNetCore => DotNetProjectType.CSharpDotNetCore,
			Database => DotNetProjectType.Database,
			DatabaseOther => DotNetProjectType.DatabaseOther,
			SolutionFolder => DotNetProjectType.SolutionFolder,
			UniversalWindowsClassLibrary => DotNetProjectType.UniversalWindowsClassLibrary,
			XamarinAndroid => DotNetProjectType.XamarinAndroid,
			XamarinAndroidBinding => DotNetProjectType.XamarinAndroidBinding,
			XamarinIos => DotNetProjectType.XamarinIos,
			XamarinIos2 => DotNetProjectType.XamarinIos,
			XamarinIosBinding => DotNetProjectType.XamarinIosBinding,
			_ => DotNetProjectType.Unknown
		};
	}

	public static string ToGuidString(DotNetProjectType type)
	{
		return type switch
		{
			DotNetProjectType.AspNet5 => AspNet5,
			DotNetProjectType.AspNetMvc1 => AspNetMvc1,
			DotNetProjectType.AspNetMvc2 => AspNetMvc2,
			DotNetProjectType.AspNetMvc3 => AspNetMvc3,
			DotNetProjectType.AspNetMvc4 => AspNetMvc4,
			DotNetProjectType.AspNetMvc5 => AspNetMvc5,
			DotNetProjectType.CPlusPlus => CPlusPlus,
			DotNetProjectType.CSharp => CSharp,
			DotNetProjectType.CSharpDotNetCore => CSharpDotNetCore,
			DotNetProjectType.Database => Database,
			DotNetProjectType.DatabaseOther => DatabaseOther,
			DotNetProjectType.SolutionFolder => SolutionFolder,
			DotNetProjectType.UniversalWindowsClassLibrary => UniversalWindowsClassLibrary,
			DotNetProjectType.XamarinAndroid => XamarinAndroid,
			DotNetProjectType.XamarinAndroidBinding => XamarinAndroidBinding,
			DotNetProjectType.XamarinIos => XamarinIos,
			DotNetProjectType.XamarinIosBinding => XamarinIosBinding,
			_ => Guid.Empty.ToString()
		};
	}

	#endregion
}