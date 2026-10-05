using System.Linq;
using XamlX.Ast;
using XamlX.Transform;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class CornerstoneXamlIlCompiledBindingsMetadataRemover : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            while (true)
            {
                if (node is NestedScopeMetadataNode nestedScope)
                    node = nestedScope.Value;
                else if (node is CornerstoneXamlIlDataContextTypeMetadataNode dataContextType)
                    node = dataContextType.Value;
                else if (node is CornerstoneXamlIlCompileBindingsNode compileBindings)
                    node = compileBindings.Value;
                else
                    return node;
            }
        }
    }
}
