using System;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX;
using XamlX.Ast;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions;

internal static class CornerstoneXamlDiagnosticCodes
{
    public const string Unknown = "CSDC9999";

    // XML/XAML parsing errors 1000-1999.
    public const string ParseError = "CSDC1000";
    public const string InvalidXAML = "CSDC1001";

    // XAML transform errors 2000-2999.
    public const string TransformError = "CSDC2000";
    public const string DuplicateXClass = "CSDC2002";
    public const string TypeSystemError = "CSDC2003";
    public const string CornerstoneIntrinsicsError = "CSDC2005";
    public const string BindingsError = "CSDC2100";
    public const string DataContextResolvingError = "CSDC2101";
    public const string StyleTransformError = "CSDC2200";
    public const string SelectorsTransformError = "CSDC2201";
    public const string PropertyPathError = "CSDC2202";
    public const string DuplicateSetterError = "CSDC2203";
    public const string StyleInMergedDictionaries = "CSDC2204";
    public const string RequiredTemplatePartMissing = "CSDC2205";
    public const string OptionalTemplatePartMissing = "CSDC2206";
    public const string TemplatePartWrongType = "CSDC2207";
    public const string ItemContainerInsideTemplate = "CSDC2208";

    // XAML emit errors 3000-3999.
    public const string EmitError = "CSDC3000";
    public const string XamlLoaderUnreachable = "CSDC3001";

    // Generator specific errors 4000-4999.
    public const string NameGeneratorError = "CSDC4001";

    // Reserved 5000-9998
    public const string Obsolete = "CSDC5001";

    internal static string XamlXDiagnosticCodeToCornerstone(object codeOrException)
    {
        return codeOrException switch
        {
            XamlXWellKnownDiagnosticCodes wellKnownDiagnosticCodes => wellKnownDiagnosticCodes switch
            {
                XamlXWellKnownDiagnosticCodes.Obsolete => Obsolete,
                _ => throw new ArgumentOutOfRangeException()
            },

            // ExperimentalAttribute reports its own code
            string code => code,

            XamlDataContextException => DataContextResolvingError,
            XamlBindingsTransformException => BindingsError,
            XamlPropertyPathException => PropertyPathError,
            XamlStyleTransformException => StyleTransformError,
            XamlSelectorsTransformException => SelectorsTransformError,

            XamlTransformException => TransformError,
            XamlTypeSystemException => TypeSystemError,
            XamlLoadException => EmitError,
            XamlParseException => ParseError,
            
            _ => Unknown
        };
    }
}
