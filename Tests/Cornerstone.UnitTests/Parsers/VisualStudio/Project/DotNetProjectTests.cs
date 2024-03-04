#region References

using System.IO;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Parsers.VisualStudio.Project;
using Cornerstone.Serialization;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.VisualStudio.Project;

[TestClass]
public class DotNetProjectTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Load()
	{
	}

	[TestMethod]
	public void LoadFile()
	{
		var path = @$"{SolutionDirectory}\Cornerstone.EntityFramework\Cornerstone.EntityFramework.csproj";
		var expected = File.ReadAllText(path);
		var project = DotNetProject.FromFile(path);
		var options = new SerializationOptions { TextFormat = TextFormat.Indented };
		project.ToJson(options).Dump();
		var actual = project.ToXml();
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void ParseFrameworks()
	{
		var content = @"<Project Sdk=""Microsoft.NET.Sdk"">
	<PropertyGroup>
		<TargetFrameworks>netstandard2.0</TargetFrameworks>
		<TargetFrameworks Condition=""$([MSBuild]::IsOSPlatform('windows'))"">$(TargetFrameworks);net8.0</TargetFrameworks>
		<AssemblyVersion>9.2.0.0</AssemblyVersion>
		<FileVersion>9.2.0.0</FileVersion>
		<Version>9.2.0.0</Version>
		<LangVersion>latest</LangVersion>
	</PropertyGroup>
</Project>";

		var project = DotNetProject.FromXml(content);
		AreEqual(
			new[] { "netstandard2.0", "net8.0" },
			project.TargetFrameworks.Select(x => x.Moniker).ToArray()
		);
	}

	[TestMethod]
	public void ParseFrameworksForClassic()
	{
		var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""12.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>1.2.345</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{4C32BC22-2421-4E17-8EBE-CF9452D6EDCF}</ProjectGuid>
    <OutputType>WinExe</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    <TargetFrameworkProfile>
    </TargetFrameworkProfile>
    <TargetPlatformVersion>8.0</TargetPlatformVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
</Project>";

		var project = DotNetProject.FromXml(content);
		AreEqual(
			new[] { "net48" },
			project.TargetFrameworks.Select(x => x.Moniker).ToArray()
		);

		// UAP (UWP)
		content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{293F3F03-6656-497D-9BA9-76E6B57C1907}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>Speedy.Application.Uwp</RootNamespace>
    <AssemblyName>Speedy.Application.Uwp</AssemblyName>
    <DefaultLanguage>en-US</DefaultLanguage>
    <TargetPlatformIdentifier>UAP</TargetPlatformIdentifier>
    <TargetPlatformVersion Condition="" '$(TargetPlatformVersion)' == '' "">10.0.22621.0</TargetPlatformVersion>
    <TargetPlatformMinVersion>10.0.19041.0</TargetPlatformMinVersion>
    <MinimumVisualStudioVersion>14</MinimumVisualStudioVersion>
    <FileAlignment>512</FileAlignment>
    <ProjectTypeGuids>{A5A43C5B-DE2A-4C0C-9213-0A381AF9435A};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
    <SuppressTfmSupportBuildWarnings>true</SuppressTfmSupportBuildWarnings>
  </PropertyGroup>
</Project>";

		project = DotNetProject.FromXml(content);
		AreEqual(
			new[] { "uap" },
			project.TargetFrameworks.Select(x => x.Moniker).ToArray()
		);
		
		content = @"<Project DefaultTargets=""Build"" ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
	<PropertyGroup>
		<Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
		<Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
		<ProjectGuid>{F24F144B-F8D4-4208-A866-9A81D54DB478}</ProjectGuid>
		<ProjectTypeGuids>{EFBA0AD7-5A72-4C68-AF49-83D382785DCF};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
		<TemplateGuid>{c9e5eea5-ca05-42a1-839b-61506e0a37df}</TemplateGuid>
		<OutputType>Library</OutputType>
		<RootNamespace>Mobile.Droid</RootNamespace>
		<AssemblyName>Mobile.Android</AssemblyName>
		<AndroidApplication>True</AndroidApplication>
		<AndroidResgenFile>Resources\Resource.designer.cs</AndroidResgenFile>
		<AndroidResgenClass>Resource</AndroidResgenClass>
		<AndroidManifest>Properties\AndroidManifest.xml</AndroidManifest>
		<MonoAndroidResourcePrefix>Resources</MonoAndroidResourcePrefix>
		<MonoAndroidAssetsPrefix>Assets</MonoAndroidAssetsPrefix>
		<TargetFrameworkVersion>v13.0</TargetFrameworkVersion>
		<AndroidEnableSGenConcurrent>true</AndroidEnableSGenConcurrent>
		<AndroidHttpClientHandlerType>Xamarin.Android.Net.AndroidClientHandler</AndroidHttpClientHandlerType>
		<NuGetPackageImportStamp />
		<AndroidManagedSymbols>true</AndroidManagedSymbols>
		<ErrorReport>prompt</ErrorReport>
		<WarningLevel>4</WarningLevel>
		<LangVersion>latest</LangVersion>
		<AndroidDexTool>d8</AndroidDexTool>
		<AndroidEnableProfiledAot>true</AndroidEnableProfiledAot>
		<AndroidPackageFormat>apk</AndroidPackageFormat>
		<AndroidUseAapt2>true</AndroidUseAapt2>
		<AndroidLinkTool>r8</AndroidLinkTool>
		<AndroidLinkMode>None</AndroidLinkMode>
		<AndroidUseSharedRuntime>false</AndroidUseSharedRuntime>
		<MandroidI18n />
		<AotAssemblies>false</AotAssemblies>
		<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>
		<EnableLLVM>false</EnableLLVM>
		<BundleAssemblies>false</BundleAssemblies>
		<NoWarn>CS0618;MSB3277;XA0125</NoWarn>
	</PropertyGroup>
</Project>";

		// Xamarin.Android?
		project = DotNetProject.FromXml(content);
		AreEqual(
			new[] { "MonoAndroid" },
			project.TargetFrameworks.Select(x => x.Moniker).ToArray()
		);

		content = @"<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
	<PropertyGroup>
		<Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
		<Platform Condition="" '$(Platform)' == '' "">iPhoneSimulator</Platform>
		<ProductVersion>8.0.30703</ProductVersion>
		<SchemaVersion>2.0</SchemaVersion>
		<ProjectGuid>{0FA31686-4A99-440E-934C-B622826D0AD1}</ProjectGuid>
		<ProjectTypeGuids>{FEACFBD2-3405-455C-9665-78FE426C6842};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
		<TemplateGuid>{6143fdea-f3c2-4a09-aafa-6e230626515e}</TemplateGuid>
		<OutputType>Exe</OutputType>
		<RootNamespace>Mobile.iOS</RootNamespace>
		<IPhoneResourcePrefix>Resources</IPhoneResourcePrefix>
		<AssemblyName>Mobile.iOS</AssemblyName>
		<MtouchEnableSGenConc>true</MtouchEnableSGenConc>
		<MtouchHttpClientHandler>NSUrlSessionHandler</MtouchHttpClientHandler>
		<ProvisioningType>automatic</ProvisioningType>
		<NoWarn>NU1605;NU1608;CS0618;MSB3277</NoWarn>
	</PropertyGroup>
</Project>";

		// Xamarin.iOS?
		project = DotNetProject.FromXml(content);
		AreEqual(
			new[] { "Xamarin.iOS" },
			project.TargetFrameworks.Select(x => x.Moniker).ToArray()
		);
	}

	#endregion
}