using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions;
using Cornerstone.Presentation.Utilities;
using Microsoft.Build.Framework;
using SPath = System.IO.Path;
namespace Cornerstone.Presentation.Build.Tasks
{
    public class GenerateCornerstoneResourcesTask : ITask
    {
        [Required]
        public ITaskItem[] Resources { get; set; }
        [Required]
        public string Root { get; set; }
        [Required]
        public string Output { get; set; }

        public string ReportImportance { get; set; }

        private MessageImportance _reportImportance;

        class Source
        {
            public string Path { get; set; }
            public int Size { get; set; }
            private byte[] _data;
            private string _sourcePath;

            public Source(ITaskItem cornerstoneResourceItem, string root)
            {
                root = SPath.GetFullPath(root);
                var relativePath = cornerstoneResourceItem.ItemSpec;
                _sourcePath = SPath.Combine(root, relativePath);
                Size = (int)new FileInfo(_sourcePath).Length;
                var link = cornerstoneResourceItem.GetMetadata("Link");
                var path = !string.IsNullOrEmpty(link)
                    ? link
                    : relativePath;
                Path = "/" + path.Replace('\\', '/');
            }

            public string SystemPath => _sourcePath ?? Path;

            public Source(string path, byte[] data)
            {
                Path = path;
                _data = data;
                Size = data.Length;
            }

            public Stream Open()
            {
                if (_data != null)
                    return new MemoryStream(_data, false);
                return File.OpenRead(_sourcePath);
            }

            public string ReadAsString()
            {
                if (_data != null)
                    return Encoding.UTF8.GetString(_data);
                return File.ReadAllText(_sourcePath);
            }
        }

        List<Source> BuildResourceSources()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<Source>();
            foreach (var r in Resources)
            {
                var src = new Source(r, Root);
                if (!seen.Add(src.Path))
                {
                    continue;
                }
                BuildEngine.LogMessage(FormattableString.Invariant($"csres -> name:{src.Path}, path: {src.SystemPath}, size:{src.Size}, ItemSpec:{r.ItemSpec}"), _reportImportance);
                list.Add(src);
            }
            return list;
        }

        private void Pack(Stream output, List<Source> sources)
        {
            CornerstoneResourcesIndexReaderWriter.WriteResources(
                output,
                sources.Select(source => new CornerstoneResourcesEntry
                {
                    Path = source.Path,
                    Size = source.Size,
                    SystemPath = source.SystemPath,
                    Open = source.Open
                }).ToList());
        }

        private bool PreProcessXamlFiles(List<Source> sources)
        {
            var typeToXamlIndex = new Dictionary<string, string>();

            foreach (var s in sources.ToArray())
            {
                var path = s.Path;
                if (path.EndsWith(".cxaml", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase) 
                    || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) 
                    || path.EndsWith(".paml", StringComparison.OrdinalIgnoreCase) )
                {
                    XamlFileInfo info;
                    try
                    {
                        info = XamlFileInfo.Parse(s.ReadAsString());
                    }
                    catch (Exception e)
                    {
                        BuildEngine.LogError(CornerstoneXamlDiagnosticCodes.InvalidXAML, s.SystemPath, "File doesn't contain valid XAML: " + e);
                        return false;
                    }

                    if (info.XClass != null)
                    {
                        if (typeToXamlIndex.ContainsKey(info.XClass))
                        {

                            BuildEngine.LogError(CornerstoneXamlDiagnosticCodes.DuplicateXClass, s.SystemPath,
                                $"Duplicate x:Class directive, {info.XClass} is already used in {typeToXamlIndex[info.XClass]}");
                            return false;
                        }
                        typeToXamlIndex[info.XClass] = path;
                    }
                }
            }

            var xamlInfo = new CornerstoneResourceXamlInfo
            {
                ClassToResourcePathIndex = typeToXamlIndex
            };
            var ms = new MemoryStream();
            new DataContractSerializer(typeof(CornerstoneResourceXamlInfo)).WriteObject(ms, xamlInfo);
            sources.Add(new Source("/!CornerstoneResourceXamlInfo", ms.ToArray()));
            return true;
        }

        public bool Execute()
        {
            Enum.TryParse(ReportImportance, true, out _reportImportance);

            BuildEngine.LogMessage($"GenerateCornerstoneResourcesTask -> Root: {Root}, {Resources?.Count()} resources, Output:{Output}", _reportImportance < MessageImportance.Low ? MessageImportance.High : _reportImportance);
            var resources = BuildResourceSources();

            if (!PreProcessXamlFiles(resources))
                return false;
            var dir = Path.GetDirectoryName(Output);
            Directory.CreateDirectory(dir);
            using (var file = File.Create(Output))
                Pack(file, resources);
            return true;
        }

        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }
    }
}
