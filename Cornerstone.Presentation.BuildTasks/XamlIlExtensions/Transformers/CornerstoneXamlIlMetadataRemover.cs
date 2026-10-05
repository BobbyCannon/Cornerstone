using XamlX.Ast;
using XamlX.Transform;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class CornerstoneXamlIlMetadataRemover : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            while (node is CornerstoneXamlIlTargetTypeMetadataNode targetType)
                node = targetType.Value;

            return node;
        }
    }
}
