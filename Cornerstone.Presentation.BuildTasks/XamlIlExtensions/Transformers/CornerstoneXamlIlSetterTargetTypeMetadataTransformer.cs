using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class CornerstoneXamlIlSetterTargetTypeMetadataTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is XamlAstObjectNode on
            && on.Children.FirstOrDefault(c => c is XamlAstXmlDirective
            {
                Namespace: XamlNamespaces.Xaml2006,
                Name: "SetterTargetType"
            }) is { } typeDirective)
        {
            var value = ((XamlAstXmlDirective)typeDirective).Values.Single();
            var type = value is XamlTypeExtensionNode typeNode ? typeNode.Value
                : value is XamlAstTextNode tn ? TypeReferenceResolver.ResolveType(context, tn.Text, false, tn, true)
                : null;
            on.Children.Remove(typeDirective);

            if (type is null)
            {
                throw new XamlTransformException("Unable to resolve SetterTargetType type", typeDirective);
            }
            return new CornerstoneXamlIlTargetTypeMetadataNode(on, type, CornerstoneXamlIlTargetTypeMetadataNode.ScopeTypes.Style);
        }
        return node;
    }
}
