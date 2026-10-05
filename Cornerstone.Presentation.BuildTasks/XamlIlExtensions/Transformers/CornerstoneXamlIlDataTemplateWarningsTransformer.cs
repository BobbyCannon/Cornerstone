using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

#if !XAMLX_INTERNAL
public
#endif
    class CornerstoneXamlIlDataTemplateWarningsTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        var cornerstoneTypes = context.GetPresentationTypes();
        var contentControl = context.GetPresentationTypes().ContentControl;

        // This transformers only looks for ContentControl delivered objects inside of DataTemplate
        if ((node is not XamlAstObjectNode objectNode)
            || !contentControl.IsAssignableFrom(objectNode.Type.GetClrType())
            || context.ParentNodes().FirstOrDefault() is not XamlAstObjectNode parentNode
            || !cornerstoneTypes.IDataTemplate.IsAssignableFrom(parentNode.Type.GetClrType()))
        {
            return node;
        }

        // And only inside of ItemTemplate or DataTemplates property value.
        if (context.ParentNodes().OfType<XamlAstXamlPropertyValueNode>().FirstOrDefault() is not { } valueNode
            || valueNode.Property.GetClrProperty() is not { } clrProperty
            || !((clrProperty.Name == "ItemTemplate" && clrProperty.DeclaringType == cornerstoneTypes.ItemsControl)
                || (clrProperty.Name == "DataTemplates" && clrProperty.DeclaringType == cornerstoneTypes.Control)))
        {
            return node;
        }

        // And only inside of ItemsControl
        if (context.ParentNodes().SkipWhile(p => p != valueNode)
                .OfType<XamlAstObjectNode>().FirstOrDefault() is not { } itemsControlNode
            || !cornerstoneTypes.ItemsControl.IsAssignableFrom(itemsControlNode.Type.GetClrType()))
        {
            return node;
        }

        // Cornerstone doesn't have any reliable way to determine container type from the API.
        if (GetKnownItemContainerTypeFullName(itemsControlNode.Type.GetClrType()) is not { } knownItemContainerTypeName
            || itemsControlNode.Type.GetClrType().Assembly?.FindType(knownItemContainerTypeName) is not { } knownItemContainerType)
        {
            return node;
        }

        if (knownItemContainerType.IsAssignableFrom(objectNode.Type.GetClrType()))
        {
            context.ReportDiagnostic(new XamlDiagnostic(
                CornerstoneXamlDiagnosticCodes.ItemContainerInsideTemplate,
                XamlDiagnosticSeverity.Warning,
                $"Unexpected '{knownItemContainerType.Name}' inside of '{itemsControlNode.Type.GetClrType().Name}.{clrProperty.Name}'. "
                + $"'{itemsControlNode.Type.GetClrType().Name}.{clrProperty.Name}' defines template of the container content, not the container itself.", node));
        }

        return node;
    }

    private static string? GetKnownItemContainerTypeFullName(IXamlType itemsControlType) => itemsControlType.FullName switch
    {
        "Cornerstone.Presentation.Controls.ListBox" => "Cornerstone.Presentation.Controls.ListBoxItem",
        "Cornerstone.Presentation.Controls.ComboBox" => "Cornerstone.Presentation.Controls.ComboBoxItem",
        "Cornerstone.Presentation.Controls.Menu" => "Cornerstone.Presentation.Controls.MenuItem",
        "Cornerstone.Presentation.Controls.MenuItem" => "Cornerstone.Presentation.Controls.MenuItem",
        "Cornerstone.Presentation.Controls.TabStrip" => "Cornerstone.Presentation.Controls.TabStripItem",
        "Cornerstone.Presentation.Controls.TabControl" => "Cornerstone.Presentation.Controls.TabItem",
        "Cornerstone.Presentation.Controls.TreeView" => "Cornerstone.Presentation.Controls.TreeViewItem",
        _ => null
    };
}
