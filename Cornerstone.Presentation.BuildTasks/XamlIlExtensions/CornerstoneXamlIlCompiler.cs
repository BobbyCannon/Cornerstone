using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Presentation.Markup.Xaml.Loader.CompilerExtensions.Transformers;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.GroupTransformers;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Parsers;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions
{
    class CornerstoneXamlIlCompiler : XamlILCompiler
    {
        private readonly IXamlType _contextType = null!;
        private readonly CornerstoneXamlIlDesignPropertiesTransformer _designTransformer;
        private readonly CornerstoneBindingExtensionTransformer _bindingTransformer;
        private readonly CornerstoneXamlIlAddSourceInfoTransformer _addSourceInfoTransformer;
        private readonly CornerstoneXamlResourceTransformer _resourceTransformer;

        private CornerstoneXamlIlCompiler(TransformerConfiguration configuration, XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings)
            : base(configuration, emitMappings, true)
        {
            void InsertAfter<T>(params IXamlAstTransformer[] t)
                => Transformers.InsertRange(Transformers.FindIndex(x => x is T) + 1, t);

            void InsertBefore<T>(params IXamlAstTransformer[] t)
                => Transformers.InsertRange(Transformers.FindIndex(x => x is T), t);

            void InsertBeforeMany(Type[] types, params IXamlAstTransformer[] t)
                => Transformers.InsertRange(types
                    .Select(type => Transformers.FindIndex(x => x.GetType() == type))
                    .Min(), t);

            // Before everything else

            Transformers.Insert(0, new XNameTransformer());
            Transformers.Insert(1, new IgnoredDirectivesTransformer());
            Transformers.Insert(2, _designTransformer = new CornerstoneXamlIlDesignPropertiesTransformer());
            Transformers.Insert(3, _bindingTransformer = new CornerstoneBindingExtensionTransformer());

            // Targeted
            InsertBefore<PropertyReferenceResolver>(
                new CornerstoneXamlIlResolveClassesPropertiesTransformer(),
                new CornerstoneXamlIlTransformInstanceAttachedProperties(),
                new CornerstoneXamlIlTransformSyntheticCompiledBindingMembers());


            InsertAfter<PropertyReferenceResolver>(
                new CornerstoneXamlIlPresentationPropertyResolver(),
                new CornerstoneXamlIlReorderClassesPropertiesTransformer(),
                new CornerstoneXamlIlClassesTransformer()
            );

            InsertBefore<ContentConvertTransformer>(
                new CornerstoneXamlIlControlThemeTransformer(),
                new CornerstoneXamlIlSelectorTransformer(),
                new CornerstoneXamlIlQueryTransformer(),
                new CornerstoneXamlIlDuplicateSettersChecker(),
                new CornerstoneXamlIlControlTemplateTargetTypeMetadataTransformer(),
                new CornerstoneXamlIlBindingPathParser(),
                new CornerstoneXamlIlSetterTargetTypeMetadataTransformer(),
                new CornerstoneXamlIlSetterTransformer(),
                new CornerstoneXamlIlStyleValidatorTransformer(),
                new CornerstoneXamlIlConstructorServiceProviderTransformer(),
                new CornerstoneXamlIlTransitionsTypeMetadataTransformer(),
                new CornerstoneXamlIlResolveByNameMarkupExtensionReplacer(),
                new CornerstoneXamlIlThemeVariantProviderTransformer(),
                new CornerstoneXamlIlDataTemplateWarningsTransformer()
            );
            InsertBefore<ConvertPropertyValuesToAssignmentsTransformer>(
                new CornerstoneXamlIlOptionMarkupExtensionTransformer());

            InsertAfter<TypeReferenceResolver>(
                new XDataTypeTransformer());

            // After everything else
            InsertBefore<NewObjectTransformer>(
                new AddNameScopeRegistration(),
                new CornerstoneXamlIlControlTemplatePartsChecker(),
                new CornerstoneXamlIlDataContextTypeTransformer(),
                new CornerstoneXamlIlBindingPathTransformer(),
                new CornerstoneXamlIlCompiledBindingsMetadataRemover()
                );

            InsertBeforeMany(new [] { typeof(DeferredContentTransformer), typeof(CornerstoneXamlIlCompiledBindingsMetadataRemover) },
                _resourceTransformer = new CornerstoneXamlResourceTransformer());

            InsertBefore<CornerstoneXamlIlTransformInstanceAttachedProperties>(new CornerstoneXamlIlTransformRoutedEvent());

            Transformers.Add(new CornerstoneXamlIlControlTemplatePriorityTransformer());
            Transformers.Add(new CornerstoneXamlIlMetadataRemover());
            Transformers.Add(new CornerstoneXamlIlEnsureResourceDictionaryCapacityTransformer());
            Transformers.Add(new CornerstoneXamlIlRootObjectScope());

            Transformers.Add(_addSourceInfoTransformer = new CornerstoneXamlIlAddSourceInfoTransformer());
            
            Emitters.Add(new CornerstoneNameScopeRegistrationXamlIlNodeEmitter());
            Emitters.Add(new CornerstoneXamlIlRootObjectScope.Emitter());
            
            GroupTransformers = new()
            {
                new XamlMergeResourceGroupTransformer(),
                new CornerstoneXamlIncludeTransformer()
            };
        }
        public CornerstoneXamlIlCompiler(TransformerConfiguration configuration,
            XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings,
            IXamlTypeBuilder<IXamlILEmitter> contextTypeBuilder)
            : this(configuration, emitMappings)
        {
            _contextType = CreateContextType(contextTypeBuilder);
        }


        public CornerstoneXamlIlCompiler(TransformerConfiguration configuration,
            XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings,
            IXamlType contextType) : this(configuration, emitMappings)
        {
            _contextType = contextType;
        }

        public const string PopulateName = "__CornerstoneXamlIlPopulate";
        public const string BuildName = "__CornerstoneXamlIlBuild";

        public bool CreateSourceInfo
        {
            get => _addSourceInfoTransformer.CreateSourceInfo || _resourceTransformer.CreateSourceInfo; 
            set => _addSourceInfoTransformer.CreateSourceInfo = _resourceTransformer.CreateSourceInfo = value; 
        }

        public bool IsDesignMode
        {
            get => _designTransformer.IsDesignMode;
            set => _designTransformer.IsDesignMode = value;
        }

        public bool DefaultCompileBindings
        {
            get => _bindingTransformer.CompileBindingsByDefault;
            set => _bindingTransformer.CompileBindingsByDefault = value;
        }

        public List<IXamlAstGroupTransformer> GroupTransformers { get; }

        public void TransformGroup(IReadOnlyCollection<IXamlDocumentResource> documents)
        {
            var ctx = new AstGroupTransformationContext(documents, _configuration);
            foreach (var transformer in GroupTransformers)
            {
                foreach (var doc in documents)
                {
                    var root = doc.XamlDocument.Root;
                    ctx.CurrentDocument = doc;
                    ctx.RootObject = (IXamlAstValueNode)root;
                    ctx.VisitChildren(ctx.RootObject, transformer);
                    root = ctx.Visit(root, transformer);

                    doc.XamlDocument.Root = root;
                }
            }
        }

#if !XAMLX_CECIL_INTERNAL
        [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
#endif
        public XamlDocument Parse(string xaml, IXamlType? overrideRootType)
        {
            var parsed = XDocumentXamlParser.Parse(xaml, new Dictionary<string, string>
            {
                {XamlNamespaces.Blend2008, XamlNamespaces.Blend2008}
            });

            var rootObject = (XamlAstObjectNode)parsed.Root;

            var classDirective = rootObject.Children
                .OfType<XamlAstXmlDirective>().FirstOrDefault(x =>
                    x.Namespace == XamlNamespaces.Xaml2006
                    && x.Name == "Class");

            var rootType =
                classDirective != null ?
                    new XamlAstClrTypeReference(classDirective,
                        _configuration.TypeSystem.GetType(((XamlAstTextNode)classDirective.Values[0]).Text),
                        false) :
                    TypeReferenceResolver.ResolveType(CreateTransformationContext(parsed),
                        (XamlAstXmlTypeReference)rootObject.Type);


            if (overrideRootType != null)
            {
                if (!rootType.Type.IsAssignableFrom(overrideRootType))
                    throw new XamlX.XamlLoadException(
                        $"Unable to substitute {rootType.Type.GetFqn()} with {overrideRootType.GetFqn()}", rootObject);
                rootType = new XamlAstClrTypeReference(rootObject, overrideRootType, false);
            }

            OverrideRootType(parsed, rootType);

            return parsed;
        }

        public void Compile(XamlDocument document, XamlDocumentTypeBuilderProvider typeBuilderProvider, string? baseUri, IFileSource? fileSource)
        {
            Compile(
                document,
                _contextType,
                typeBuilderProvider.PopulateMethod,
                typeBuilderProvider.PopulateDeclaringType,
                typeBuilderProvider.BuildMethod,
                typeBuilderProvider.BuildDeclaringType,
                _configuration.TypeMappings.XmlNamespaceInfoProvider == null ?
                    null :
                    typeBuilderProvider.PopulateDeclaringType.DefineSubType(_configuration.WellKnownTypes.Object, "__CornerstoneXamlIlNsInfo", XamlVisibility.Private),
                baseUri,
                fileSource);
        }

#if !XAMLX_CECIL_INTERNAL
        [RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
#endif
        public void ParseAndCompile(string xaml, string? baseUri, IFileSource fileSource, IXamlTypeBuilder<IXamlILEmitter> tb, IXamlType overrideRootType)
        {
            var parsed = Parse(xaml, overrideRootType);

            Transform(parsed);
            Compile(parsed, tb, _contextType, PopulateName, BuildName, "__CornerstoneXamlIlNsInfo", baseUri, fileSource);
        }

        public void OverrideRootType(XamlDocument doc, IXamlAstTypeReference newType)
        {
            var root = (XamlAstObjectNode)doc.Root;
            var oldType = root.Type;
            if (oldType.Equals(newType))
                return;

            root.Type = newType;
            foreach (var child in root.Children.OfType<XamlAstXamlPropertyValueNode>())
            {
                if (child.Property is XamlAstNamePropertyReference prop)
                {
                    if (prop.DeclaringType.Equals(oldType))
                        prop.DeclaringType = newType;
                    if (prop.TargetType.Equals(oldType))
                        prop.TargetType = newType;
                }
            }
        }
    }
}
