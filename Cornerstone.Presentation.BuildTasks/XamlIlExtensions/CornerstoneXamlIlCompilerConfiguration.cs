using Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions
{
    class CornerstoneXamlIlCompilerConfiguration : TransformerConfiguration
    {
        public XamlIlClrPropertyInfoEmitter ClrPropertyEmitter { get; }
        public XamlIlPropertyInfoAccessorFactoryEmitter AccessorFactoryEmitter { get; }
        public XamlIlTrampolineBuilder TrampolineBuilder { get; }

        public CornerstoneXamlIlCompilerConfiguration(
            IXamlTypeSystem typeSystem,
            IXamlAssembly? defaultAssembly,
            XamlLanguageTypeMappings typeMappings,
            XamlXmlnsMappings? xmlnsMappings,
            XamlValueConverter? customValueConverter,
            XamlIlClrPropertyInfoEmitter clrPropertyEmitter,
            XamlIlPropertyInfoAccessorFactoryEmitter accessorFactoryEmitter,
            XamlIlTrampolineBuilder trampolineBuilder,
            IXamlIdentifierGenerator? identifierGenerator,
            XamlDiagnosticsHandler? diagnosticsHandler)
            : base(typeSystem, defaultAssembly, typeMappings, xmlnsMappings, customValueConverter, identifierGenerator, diagnosticsHandler)
        {
            ClrPropertyEmitter = clrPropertyEmitter;
            AccessorFactoryEmitter = accessorFactoryEmitter;
            TrampolineBuilder = trampolineBuilder;
            AddExtra(ClrPropertyEmitter);
            AddExtra(AccessorFactoryEmitter);
            AddExtra(TrampolineBuilder);
            AddExtra(new CornerstoneXamlIlWellKnownTypes(TypeSystem));
        }
    }
}
