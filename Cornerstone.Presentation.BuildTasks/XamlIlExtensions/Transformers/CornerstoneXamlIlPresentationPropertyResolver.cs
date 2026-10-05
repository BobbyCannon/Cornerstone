using System.Linq;
using XamlX.Ast;
using XamlX.Transform;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class CornerstoneXamlIlPresentationPropertyResolver : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstClrProperty prop)
            {
                var n = prop.Name + "Property";
                var field =
                    prop.DeclaringType.Fields
                    .FirstOrDefault(f => f.Name == n);
                if (field != null)
                    return new XamlIlPresentationProperty(prop, field, context.GetPresentationTypes());
            }

            return node;
        }
    }
}
