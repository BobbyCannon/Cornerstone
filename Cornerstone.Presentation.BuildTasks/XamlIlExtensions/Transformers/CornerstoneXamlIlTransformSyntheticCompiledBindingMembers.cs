using System;
using System.Collections.Generic;
using System.Text;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{
    class CornerstoneXamlIlTransformSyntheticCompiledBindingMembers : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstNamePropertyReference prop
               && prop.TargetType is XamlAstClrTypeReference targetRef
               && targetRef.GetClrType().Equals(context.GetPresentationTypes().CompiledBindingExtension))
            {
                if (prop.Name == "ElementName")
                {
                    return new CornerstoneSyntheticCompiledBindingProperty(node,
                        SyntheticCompiledBindingPropertyName.ElementName);
                }
                else if (prop.Name == "RelativeSource")
                {
                    return new CornerstoneSyntheticCompiledBindingProperty(node,
                        SyntheticCompiledBindingPropertyName.RelativeSource);
                }
            }

            return node;
        }
    }

    enum SyntheticCompiledBindingPropertyName
    {
        ElementName,
        RelativeSource
    }

    class CornerstoneSyntheticCompiledBindingProperty : XamlAstNode, IXamlAstPropertyReference
    {
        public SyntheticCompiledBindingPropertyName Name { get; }

        public CornerstoneSyntheticCompiledBindingProperty(
            IXamlLineInfo lineInfo,
            SyntheticCompiledBindingPropertyName name)
            : base(lineInfo)
        {
            Name = name;
        }
    }
}
