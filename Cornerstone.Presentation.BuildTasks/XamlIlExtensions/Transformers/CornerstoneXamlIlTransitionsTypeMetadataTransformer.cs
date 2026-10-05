using XamlX.Ast;
using XamlX.Transform;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class CornerstoneXamlIlTransitionsTypeMetadataTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstObjectNode on)
            {
                foreach (var ch in on.Children)
                {
                    if (ch is XamlAstXamlPropertyValueNode pn
                        && pn.Property.GetClrProperty().Getter?.ReturnType.Equals(context.GetPresentationTypes().Transitions) == true)
                    {
                        for (var c = 0; c < pn.Values.Count; c++)
                        {
                            pn.Values[c] = new CornerstoneXamlIlTargetTypeMetadataNode(pn.Values[c], on.Type,
                                CornerstoneXamlIlTargetTypeMetadataNode.ScopeTypes.Transitions);
                        }
                    }
                }
            }
            return node;
        }
    }
}
