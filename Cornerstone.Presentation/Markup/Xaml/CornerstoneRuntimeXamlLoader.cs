using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Cornerstone.Presentation.Markup.Xaml
{
    /// <summary>
    /// Loads XAML from a string or stream via <see cref="CornerstoneXamlLoader.IRuntimeXamlLoader"/>.
    /// </summary>
    public static class CornerstoneRuntimeXamlLoader
    {
        public static object Load(string xaml, Assembly localAssembly = null, object rootInstance = null, Uri uri = null, bool designMode = false)
        {
            if (xaml == null)
                throw new ArgumentNullException(nameof(xaml));

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
            return Load(stream, localAssembly, rootInstance, uri, designMode);
        }

        public static object Load(Stream stream, Assembly localAssembly = null, object rootInstance = null, Uri uri = null, bool designMode = false)
        {
            var runtimeLoader = PresentationLocator.Current.GetService<CornerstoneXamlLoader.IRuntimeXamlLoader>();
            if (runtimeLoader == null)
            {
                throw new XamlLoadException(
                    "No IRuntimeXamlLoader registered. Runtime XAML load requires a runtime loader.");
            }

            var document = new RuntimeXamlLoaderDocument(uri, rootInstance, stream);
            var configuration = new RuntimeXamlLoaderConfiguration { DesignMode = designMode, LocalAssembly = localAssembly };
            return runtimeLoader.Load(document, configuration);
        }

        public static object Parse(string xaml, Assembly localAssembly = null)
            => Load(xaml, localAssembly);

        public static T Parse<T>(string xaml, Assembly localAssembly = null)
            => (T)Parse(xaml, localAssembly);

        public static object Load(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration configuration = null)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var runtimeLoader = PresentationLocator.Current.GetService<CornerstoneXamlLoader.IRuntimeXamlLoader>();
            if (runtimeLoader == null)
            {
                throw new XamlLoadException(
                    "No IRuntimeXamlLoader registered. Runtime XAML load requires a runtime loader.");
            }

            return runtimeLoader.Load(document, configuration ?? new RuntimeXamlLoaderConfiguration());
        }
    }
}
