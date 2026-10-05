using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class CornerstoneXamlIlStyleValidatorTransformer : IXamlAstTransformer
{
    // See issues/7461
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (!(node is XamlAstObjectNode on
              && context.GetPresentationTypes().IStyle.IsAssignableFrom(on.Type.GetClrType())))
            return node;

        if (context.ParentNodes().FirstOrDefault() is XamlAstXamlPropertyValueNode propertyValueNode
            && propertyValueNode.Property.GetClrProperty() is { } clrProperty
            && clrProperty.Name == "MergedDictionaries"
            && clrProperty.DeclaringType == context.GetPresentationTypes().ResourceDictionary)
        {
            var nodeName = on.Type.GetClrType().Name;
            context.ReportDiagnostic(new XamlDiagnostic(
                CornerstoneXamlDiagnosticCodes.StyleInMergedDictionaries,
                XamlDiagnosticSeverity.Warning,
                // Keep it single line, as MSBuild splits multiline warnings into two warnings.
                $"Including {nodeName} as part of MergedDictionaries will ignore any nested styles." +
                $"Instead, you can add {nodeName} to the Styles collection on the same control or application.",
                node));
        }
        
        return node;
    }
}
