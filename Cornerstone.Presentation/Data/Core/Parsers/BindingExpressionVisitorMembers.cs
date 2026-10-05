using System;
using System.Reflection;

namespace Cornerstone.Presentation.Data.Core.Parsers;

/// <summary>
/// Stores reflection members used by <see cref="BindingExpressionVisitor{TIn}"/> outside of the
/// generic class to avoid duplication for each generic instantiation.
/// </summary>
internal static class BindingExpressionVisitorMembers
{
    static BindingExpressionVisitorMembers()
    {
        PresentationObjectIndexer = typeof(PresentationObject).GetProperty(CommonPropertyNames.IndexerName, [typeof(PresentationProperty)])!;
        CreateDelegateMethod = typeof(MethodInfo).GetMethod(nameof(MethodInfo.CreateDelegate), [typeof(Type), typeof(object)])!;
    }

    public static readonly PropertyInfo PresentationObjectIndexer;
    public static readonly MethodInfo CreateDelegateMethod;
}
