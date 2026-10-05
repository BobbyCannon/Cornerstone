using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cornerstone.Presentation.Utilities;

namespace Cornerstone.Presentation.Platform.Internal;

internal interface IAssemblyDescriptor
{
    Assembly Assembly { get; }
    Dictionary<string, IAssetDescriptor>? Resources { get; }
    Dictionary<string, IAssetDescriptor>? CornerstoneResources { get; }
    string? Name { get; }
}

internal class AssemblyDescriptor : IAssemblyDescriptor
{
    public AssemblyDescriptor(Assembly assembly)
    {
        Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        Resources = assembly.GetManifestResourceNames()
            .ToDictionary(n => n, n => (IAssetDescriptor)new AssemblyResourceDescriptor(assembly, n));
        Name = assembly.GetName().Name;

        using var resources = assembly.GetManifestResourceStream(Constants.CornerstoneResourceName);
        if (resources != null)
        {
            Resources.Remove(Constants.CornerstoneResourceName);

            var indexLength = new BinaryReader(resources).ReadInt32();
            var index = CornerstoneResourcesIndexReaderWriter.ReadIndex(new SlicedStream(resources, 4, indexLength));
            var baseOffset = indexLength + 4;
            CornerstoneResources = index.ToDictionary(GetPathRooted, r => (IAssetDescriptor)
                new CornerstoneResourceDescriptor(assembly, baseOffset + r.Offset, r.Size));
        }
    }

    public Assembly Assembly { get; }
    public Dictionary<string, IAssetDescriptor>? Resources { get; }
    public Dictionary<string, IAssetDescriptor>? CornerstoneResources { get; }
    public string? Name { get; }

    private static string GetPathRooted(CornerstoneResourcesIndexEntry r) =>
        r.Path![0] == '/' ? r.Path : '/' + r.Path;
}
