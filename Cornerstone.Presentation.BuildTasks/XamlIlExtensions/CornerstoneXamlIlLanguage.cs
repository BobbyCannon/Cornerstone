using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions
{
    /*
        This file is used in the build task.
        ONLY use types from netstandard and XamlIl. NO dependencies on Cornerstone are allowed. Only strings.
        No, nameof isn't welcome here either
     */
    
    class CornerstoneXamlIlLanguage
    {
        [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrCornerstoneAssembly)]
        public static (XamlLanguageTypeMappings language, XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emit) Configure(IXamlTypeSystem typeSystem)
        {
            var runtimeHelpers = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime.XamlIlRuntimeHelpers");
            var rv = new XamlLanguageTypeMappings(typeSystem)
            {
                SupportInitialize = typeSystem.GetType("System.ComponentModel.ISupportInitialize"),
                XmlnsAttributes =
                {
                    typeSystem.GetType("Cornerstone.Presentation.Metadata.XmlnsDefinitionAttribute"),
                },
                ContentAttributes =
                {
                    typeSystem.GetType("Cornerstone.Presentation.Metadata.ContentAttribute")
                },
                WhitespaceSignificantCollectionAttributes =
                {
                    typeSystem.GetType("Cornerstone.Presentation.Metadata.WhitespaceSignificantCollectionAttribute")
                },
                TrimSurroundingWhitespaceAttributes =
                {
                    typeSystem.GetType("Cornerstone.Presentation.Metadata.TrimSurroundingWhitespaceAttribute")
                },
                ProvideValueTarget = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.IProvideValueTarget"),
                RootObjectProvider = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.IRootObjectProvider"),
                RootObjectProviderIntermediateRootPropertyName = "IntermediateRootObject",
                UriContextProvider = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.IUriContext"),
                ParentStackProvider =
                    typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime.ICornerstoneXamlIlParentStackProvider"),

                XmlNamespaceInfoProvider =
                    typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime.ICornerstoneXamlIlXmlNamespaceInfoProvider"),
                DeferredContentPropertyAttributes = { typeSystem.GetType("Cornerstone.Presentation.Metadata.TemplateContentAttribute") },
                DeferredContentExecutorCustomizationDefaultTypeParameter = typeSystem.GetType("Cornerstone.Presentation.Controls.Control"),
                DeferredContentExecutorCustomizationTypeParameterDeferredContentAttributePropertyNames = new List<string>
                {
                    "TemplateResultType"
                },
                DeferredContentExecutorCustomization =
                    runtimeHelpers.FindMethod(m => m.Name == "DeferredTransformationFactoryV3"),
                UsableDuringInitializationAttributes =
                {
                    typeSystem.GetType("Cornerstone.Presentation.Metadata.UsableDuringInitializationAttribute"),
                },
                InnerServiceProviderFactoryMethod =
                    runtimeHelpers.FindMethod(m => m.Name == "CreateInnerServiceProviderV1"),
                IAddChild = typeSystem.GetType("Cornerstone.Presentation.Metadata.IAddChild"),
                IAddChildOfT = typeSystem.GetType("Cornerstone.Presentation.Metadata.IAddChild`1")
            };
            rv.CustomAttributeResolver = new AttributeResolver(typeSystem, rv);

            var nameScopeType = typeSystem.GetType("Cornerstone.Presentation.Controls.Naming.INameScope");
            var eagerParentStackProviderInterfaceType = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime.ICornerstoneXamlIlEagerParentStackProvider");

            var emit = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>
            {
                ProvideValueTargetPropertyEmitter = XamlIlPresentationPropertyHelper.EmitProvideValueTarget,
                ContextTypeBuilderCallback = definition =>
                {
                    EmitNameScopeField(rv, typeSystem, definition, nameScopeType);
                    EmitEagerParentStackProvider(rv, typeSystem, definition, runtimeHelpers, eagerParentStackProviderInterfaceType);
                }
            };
            return (rv, emit);
        }

        public const string ContextNameScopeFieldName = "CornerstoneNameScope";

        [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrCornerstoneAssembly)]
        private static void EmitNameScopeField(
            XamlLanguageTypeMappings mappings,
            IXamlTypeSystem typeSystem,
            IXamlILContextDefinition<IXamlILEmitter> definition,
            IXamlType nameScopeType)
        {
            var field = definition.TypeBuilder.DefineField(nameScopeType,
                ContextNameScopeFieldName, XamlVisibility.Public, false);
            definition.ConstructorBuilder.Generator
                .Ldarg_0()
                .Ldarg(1)
                .Ldtype(nameScopeType)
                .EmitCall(mappings.ServiceProvider.GetMethod(new FindMethodMethodSignature("GetService",
                    typeSystem.WellKnownTypes.Object, typeSystem.WellKnownTypes.Type)))
                .Stfld(field);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrCornerstoneAssembly)]
        private static void EmitEagerParentStackProvider(
            XamlLanguageTypeMappings mappings,
            IXamlTypeSystem typeSystem,
            IXamlILContextDefinition<IXamlILEmitter> definition,
            IXamlType runtimeHelpers,
            IXamlType interfaceType)
        {
            definition.TypeBuilder.AddInterfaceImplementation(interfaceType);

            // IReadOnlyList<object> DirectParentsStack => (IReadOnlyList<object>)ParentsStack;
            var directParentsGetter = ImplementInterfacePropertyGetter("DirectParentsStack");
            directParentsGetter.Generator
                .LdThisFld(definition.ParentListField!)
                .Castclass(directParentsGetter.ReturnType)
                .Ret();

            var serviceProviderGetServiceMethod = mappings.ServiceProvider.GetMethod(new FindMethodMethodSignature(
                "GetService",
                typeSystem.WellKnownTypes.Object,
                typeSystem.WellKnownTypes.Type));

            var asEagerParentStackProviderMethod = runtimeHelpers.GetMethod(new FindMethodMethodSignature(
                "AsEagerParentStackProvider",
                interfaceType,
                mappings.ParentStackProvider!)
            {
                IsStatic = true
            });

            // ICornerstoneXamlIlEagerParentStackProvider? ParentProvider
            // => XamlIlRuntimeHelpers.AsEagerParentStackProvider(_serviceProvider.GetService(typeof(ICornerstoneXamlIlParentStackProvider)));
            var parentProviderGetter = ImplementInterfacePropertyGetter("ParentProvider");
            parentProviderGetter.Generator
                .LdThisFld(definition.ParentServiceProviderField)
                .Ldtype(mappings.ParentStackProvider!)
                .EmitCall(serviceProviderGetServiceMethod)
                .EmitCall(asEagerParentStackProviderMethod)
                .Ret();

            IXamlMethodBuilder<IXamlILEmitter> ImplementInterfacePropertyGetter(string propertyName)
            {
                var interfaceGetter = interfaceType.GetMethod(m => m.Name == "get_" + propertyName);

                var getter = definition.TypeBuilder.DefineMethod(
                    interfaceGetter.ReturnType,
                    Array.Empty<IXamlType>(),
                    "get_" + propertyName,
                    XamlVisibility.Private,
                    false,
                    true,
                    interfaceGetter);

                definition.TypeBuilder.DefineProperty(interfaceGetter.ReturnType, propertyName, null, getter);

                return getter;
            }
        }

        class AttributeResolver : IXamlCustomAttributeResolver
        {
            private readonly IXamlType _typeConverterAttribute;

            private readonly List<KeyValuePair<IXamlType, IXamlType>> _converters =
                new List<KeyValuePair<IXamlType, IXamlType>>();

            private readonly IXamlType _cornerstoneList;
            private readonly IXamlType _cornerstoneListConverter;


            [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrCornerstoneAssembly)]
            public AttributeResolver(IXamlTypeSystem typeSystem, XamlLanguageTypeMappings mappings)
            {
                _typeConverterAttribute = mappings.TypeConverterAttributes.First();

                void AddType(IXamlType type, IXamlType conv) 
                    => _converters.Add(new KeyValuePair<IXamlType, IXamlType>(type, conv));
                
                AddType(typeSystem.GetType("Cornerstone.Presentation.Media.IImage"), typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.Converters.BitmapTypeConverter"));
                AddType(typeSystem.GetType("Cornerstone.Presentation.Media.Imaging.Bitmap"), typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.Converters.BitmapTypeConverter"));
                AddType(typeSystem.GetType("Cornerstone.Presentation.Media.IImageBrushSource"), typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.Converters.BitmapTypeConverter"));
                AddType(typeSystem.WellKnownTypes.IListOfT.MakeGenericType(typeSystem.GetType("Cornerstone.Presentation.Point")),
                    typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.Converters.PointsListTypeConverter"));
                AddType(typeSystem.GetType("Cornerstone.Presentation.Controls.Chrome.WindowIcon"), typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.Converters.IconTypeConverter"));
                AddType(typeSystem.GetType("System.Globalization.CultureInfo"), typeSystem.GetType( "System.ComponentModel.CultureInfoConverter"));
                AddType(typeSystem.WellKnownTypes.Uri, typeSystem.GetType( "Cornerstone.Presentation.Markup.Xaml.Converters.PresentationUriTypeConverter"));
                AddType(typeSystem.GetType("System.TimeSpan"), typeSystem.GetType( "Cornerstone.Presentation.Markup.Xaml.Converters.TimeSpanTypeConverter"));
                AddType(typeSystem.GetType("Cornerstone.Presentation.Media.FontFamily"), typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.Converters.FontFamilyTypeConverter"));
                _cornerstoneList = typeSystem.GetType("Cornerstone.Presentation.Collections.OldPresentationList`1");
                _cornerstoneListConverter = typeSystem.GetType("Cornerstone.Presentation.Collections.OldPresentationListConverter`1");
            }

            IXamlType? LookupConverter(IXamlType type)
            {
                foreach(var p in _converters)
                    if (p.Key.Equals(type))
                        return p.Value;
                if (type.GenericTypeDefinition?.Equals(_cornerstoneList) == true)
                    return _cornerstoneListConverter.MakeGenericType(type.GenericArguments[0]);
                return null;
            }

            class ConstructedAttribute : IXamlCustomAttribute
            {
                public bool Equals(IXamlCustomAttribute? other) => false;
                
                public IXamlType Type { get; }
                public List<object?> Parameters { get; }
                public Dictionary<string, object?> Properties { get; }

                public ConstructedAttribute(IXamlType type, List<object?>? parameters, Dictionary<string, object?>? properties)
                {
                    Type = type;
                    Parameters = parameters ?? new List<object?>();
                    Properties = properties ?? new Dictionary<string, object?>();
                }
            }
            
            public IXamlCustomAttribute? GetCustomAttribute(IXamlType type, IXamlType attributeType)
            {
                if (attributeType.Equals(_typeConverterAttribute))
                {
                    var conv = LookupConverter(type);
                    if (conv != null)
                        return new ConstructedAttribute(_typeConverterAttribute, [conv], null);
                }

                return null;
            }

            public IXamlCustomAttribute? GetCustomAttribute(IXamlProperty property, IXamlType attributeType)
            {
                return null;
            }
        }

        public static bool CustomValueConverter(
            AstTransformationContext context,
            IXamlAstValueNode node,
            IReadOnlyList<IXamlCustomAttribute>? customAttributes,
            IXamlType type,
            [NotNullWhen(true)] out IXamlAstValueNode? result)
        {
            if (node is CornerstoneXamlIlOptionMarkupExtensionTransformer.OptionsMarkupExtensionNode optionsNode)
            {
                if (optionsNode.ConvertToReturnType(context, type, out var newOptionsNode))
                {
                    result = newOptionsNode;
                    return true;
                }
            }

            if (!(node is XamlAstTextNode textNode))
            {
                result = null;
                return false;
            }

            var text = textNode.Text;
            var types = context.GetPresentationTypes();

            if (CornerstoneXamlIlLanguageParseIntrinsics.TryConvert(context, node, text, type, types, out result))
            {
                return true;
            }

            if (type.Is("Cornerstone.Presentation", "PresentationProperty"))
            {
                var attrType = context.GetPresentationTypes().InheritDataTypeFromAttribute;
                var scopeKind = customAttributes?
                        .FirstOrDefault(a => a.Type.Equals(attrType))?.Parameters
                        .FirstOrDefault() switch
                    {
                        1 => CornerstoneXamlIlTargetTypeMetadataNode.ScopeTypes.Style,
                        2 => CornerstoneXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate,
                        _ => (CornerstoneXamlIlTargetTypeMetadataNode.ScopeTypes?)null
                    };

                var scope = context.ParentNodes().OfType<CornerstoneXamlIlTargetTypeMetadataNode>()
                    .FirstOrDefault(s => scopeKind.HasValue ? s.ScopeType == scopeKind : true);
                if (scope == null)
                {
#if NET6_0_OR_GREATER
                    var isScopeDefined = Enum.IsDefined<CornerstoneXamlIlTargetTypeMetadataNode.ScopeTypes>(scopeKind ?? default);
#else
                    var isScopeDefined = Enum.IsDefined(typeof(CornerstoneXamlIlTargetTypeMetadataNode.ScopeTypes), scopeKind ?? default);
#endif
                    var scopeKindStr = isScopeDefined ? scopeKind!.Value.ToString() : "parent"; 
                    throw new XamlX.XamlLoadException($"Unable to find the {scopeKindStr} scope for PresentationProperty lookup", node);
                }

                result = XamlIlPresentationPropertyHelper.CreateNode(context, text, scope.TargetType, node );
                return true;
            }

            result = null;
            return false;
        }
    }
}
