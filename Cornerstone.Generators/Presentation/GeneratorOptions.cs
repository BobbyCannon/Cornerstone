using System;
using Cornerstone.Generators.Presentation.Common;
using Cornerstone.Generators.Presentation.Common.Domain;
using Cornerstone.Generators.Presentation.NameGenerator;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cornerstone.Generators.Presentation;

// When update these enum values, don't forget to update Cornerstone.Presentation.Generators.props.
internal enum BuildProperties
{
    CornerstoneNameGeneratorIsEnabled = 0,
    CornerstoneNameGeneratorBehavior = 1,
    CornerstoneNameGeneratorDefaultFieldModifier = 2,
    CornerstoneNameGeneratorFilterByPath = 3,
    CornerstoneNameGeneratorFilterByNamespace = 4,
    CornerstoneNameGeneratorViewFileNamingStrategy = 5,
    CornerstoneNameGeneratorAttachDevTools = 6,
    CornerstonePropertyGeneratorIsEnabled = 7,
    // TODO add other generators properties here.
}

internal record GeneratorOptions
{
    public GeneratorOptions(AnalyzerConfigOptions options)
    {
        CornerstoneNameGeneratorIsEnabled = GetBoolProperty(
            options,
            BuildProperties.CornerstoneNameGeneratorIsEnabled,
            true);
        CornerstoneNameGeneratorBehavior = GetEnumProperty(
            options,
            BuildProperties.CornerstoneNameGeneratorBehavior,
            Behavior.InitializeComponent);
        CornerstoneNameGeneratorClassFieldModifier = GetEnumProperty(
            options,
            BuildProperties.CornerstoneNameGeneratorDefaultFieldModifier,
            NamedFieldModifier.Internal);
        CornerstoneNameGeneratorViewFileNamingStrategy = GetEnumProperty(
            options,
            BuildProperties.CornerstoneNameGeneratorViewFileNamingStrategy,
            ViewFileNamingStrategy.NamespaceAndClassName);
        CornerstoneNameGeneratorFilterByPath = new GlobPatternGroup(GetStringArrayProperty(
            options,
            BuildProperties.CornerstoneNameGeneratorFilterByPath,
            "*"));
        CornerstoneNameGeneratorFilterByNamespace = new GlobPatternGroup(GetStringArrayProperty(
            options,
            BuildProperties.CornerstoneNameGeneratorFilterByNamespace,
            "*"));
        CornerstoneNameGeneratorAttachDevTools = GetBoolProperty(
            options,
            BuildProperties.CornerstoneNameGeneratorAttachDevTools,
            true);
        CornerstonePropertyGeneratorIsEnabled = GetBoolProperty(
            options,
            BuildProperties.CornerstonePropertyGeneratorIsEnabled,
            true);
    }

    public bool CornerstoneNameGeneratorIsEnabled { get; }
    
    public Behavior CornerstoneNameGeneratorBehavior { get; }

    public NamedFieldModifier CornerstoneNameGeneratorClassFieldModifier { get; }

    public ViewFileNamingStrategy CornerstoneNameGeneratorViewFileNamingStrategy { get; }

    public IGlobPattern CornerstoneNameGeneratorFilterByPath { get; }

    public IGlobPattern CornerstoneNameGeneratorFilterByNamespace { get; }

    public bool CornerstoneNameGeneratorAttachDevTools { get; }

    public bool CornerstonePropertyGeneratorIsEnabled { get; }

    private static string[] GetStringArrayProperty(AnalyzerConfigOptions options, BuildProperties name, string defaultValue)
    {
        var key = name.ToString();
        var value = options.GetMsBuildProperty(key, defaultValue);
        return value.Contains(";") ? value.Split(';') : [value];
    }

    private static TEnum GetEnumProperty<TEnum>(AnalyzerConfigOptions options, BuildProperties name, TEnum defaultValue) where TEnum : struct
    {
        var key = name.ToString();
        var value = options.GetMsBuildProperty(key, defaultValue.ToString());
        return Enum.TryParse(value, true, out TEnum behavior) ? behavior : defaultValue;
    }

    private static bool GetBoolProperty(AnalyzerConfigOptions options, BuildProperties name, bool defaultValue)
    {
        var key = name.ToString();
        var value = options.GetMsBuildProperty(key, defaultValue.ToString());
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
}
