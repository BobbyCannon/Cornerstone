#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cornerstone.Parsers.VisualStudio.Solution;

public enum DotNetProjectType
{
	Unknown,
	AspNet5,
	AspNetMvc1,
	AspNetMvc2,
	AspNetMvc3,
	AspNetMvc4,
	AspNetMvc5,
	CPlusPlus,
	CSharp,
	CSharpDotNetCore,
	Database,
	DatabaseOther,
	SolutionFolder,
	UniversalWindowsClassLibrary,
	XamarinAndroid,
	XamarinAndroidBinding,
	XamarinIos,
	XamarinIosBinding
}