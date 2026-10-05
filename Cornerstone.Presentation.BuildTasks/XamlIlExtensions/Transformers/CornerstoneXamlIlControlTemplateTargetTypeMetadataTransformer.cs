using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class CornerstoneXamlIlControlTemplateTargetTypeMetadataTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (!(node is XamlAstObjectNode on
                  && ControlTemplateScopeCache.GetOrCreate(context).IsControlTemplateScope(on.Type.GetClrType())))
                return node;

            var tt = on.Children.OfType<XamlAstXamlPropertyValueNode>().FirstOrDefault(ch =>
                                              ch.Property.GetClrProperty().Name == "TargetType");

            if (context.ParentNodes().FirstOrDefault() is CornerstoneXamlIlTargetTypeMetadataNode)
                // Deja vu. I've just been in this place before
                return node;

            IXamlAstTypeReference targetType;

            var templatableBaseType = context.GetPresentationTypes().Control;

            targetType = tt?.Values.FirstOrDefault() switch
            {
                XamlTypeExtensionNode tn => tn.Value,
                XamlAstTextNode textNode => TypeReferenceResolver.ResolveType(context, textNode.Text, false, textNode, true),
                _ when context.ParentNodes()
                    .OfType<CornerstoneXamlIlTargetTypeMetadataNode>()
                    .FirstOrDefault() is { ScopeType: CornerstoneXamlIlTargetTypeMetadataNode.ScopeTypes.Style } parentScope => parentScope.TargetType,
                _ when context.ParentNodes().Skip(1).FirstOrDefault() is XamlAstObjectNode directParentNode
                         && templatableBaseType.IsAssignableFrom(directParentNode.Type.GetClrType()) => directParentNode.Type,
                _ => new XamlAstClrTypeReference(node,
                        templatableBaseType, false)
            };

            return new CornerstoneXamlIlTargetTypeMetadataNode(on, targetType,
                CornerstoneXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate);
        }

        private sealed class ControlTemplateScopeCache
        {
            private readonly IXamlType _controlTemplateScopeAttributeType;
            private readonly Dictionary<IXamlType, bool> _isScopeByType = new();

            private ControlTemplateScopeCache(IXamlType controlTemplateScopeAttributeType)
                => _controlTemplateScopeAttributeType = controlTemplateScopeAttributeType;

            public static ControlTemplateScopeCache GetOrCreate(AstTransformationContext context)
            {
                if (!context.TryGetItem(out ControlTemplateScopeCache? cache))
                {
                    cache = new ControlTemplateScopeCache(context.GetPresentationTypes().ControlTemplateScopeAttribute);
                    context.SetItem(cache);
                }

                return cache;
            }

            private bool HasScopeAttribute(IXamlType type)
                => type.CustomAttributes.Any(attr => attr.Type == _controlTemplateScopeAttributeType);

            private bool IsControlTemplateScopeCore(IXamlType type)
            {
                for (var t = type; t is not null; t = t.BaseType)
                {
                    if (HasScopeAttribute(t))
                        return true;
                }

                foreach (var iface in type.Interfaces)
                {
                    if (HasScopeAttribute(iface))
                        return true;
                }

                return false;
            }

            public bool IsControlTemplateScope(IXamlType type)
            {
                if (!_isScopeByType.TryGetValue(type, out var isScope))
                {
                    isScope = IsControlTemplateScopeCore(type);
                    _isScopeByType[type] = isScope;
                }

                return isScope;
            }
        }
    }

    class CornerstoneXamlIlTargetTypeMetadataNode : XamlValueWithSideEffectNodeBase
    {
        public IXamlAstTypeReference TargetType { get; set; }
        public ScopeTypes ScopeType { get; }

        public enum ScopeTypes
        {
            Style = 1,
            ControlTemplate,
            Transitions,
            Container
        }

        public CornerstoneXamlIlTargetTypeMetadataNode(IXamlAstValueNode value, IXamlAstTypeReference targetType,
            ScopeTypes type)
            : base(value, value)
        {
            TargetType = targetType;
            ScopeType = type;
        }
    }
}
