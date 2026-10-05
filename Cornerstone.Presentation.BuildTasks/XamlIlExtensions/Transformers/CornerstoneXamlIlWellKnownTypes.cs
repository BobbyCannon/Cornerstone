using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.GroupTransformers;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;
using Cornerstone.Presentation.Controls.Layout;

namespace Cornerstone.Presentation.Markup.Xaml.XamlIl.CompilerExtensions.Transformers
{

    sealed class CornerstoneXamlIlWellKnownTypes
    {
        public IXamlType RuntimeHelpers { get; }
        public IXamlType PresentationObject { get; }
        public IXamlType BindingPriority { get; }
        public IXamlType PresentationObjectExtensions { get; }
        public IXamlType PresentationProperty { get; }
        public IXamlType PresentationPropertyT { get; }
        public IXamlType StyledPropertyT { get; }
        public IXamlMethod PresentationObjectSetStyledPropertyValue { get; }
        public IXamlType CornerstoneAttachedPropertyT { get; }
        public IXamlType BindingBase { get; }
        public IXamlType BindingExpressionBase { get; }
        public IXamlType MultiBinding { get; }
        public IXamlMethod PresentationObjectBindMethod { get; }
        public IXamlMethod PresentationObjectSetValueMethod { get; }
        public IXamlType IDisposable { get; }
        public IXamlType ICommand { get; }
        public XamlTypeWellKnownTypes XamlIlTypes { get; }
        public IXamlType Transitions { get; }
        public IXamlType AssignBindingAttribute { get; }
        public IXamlType DependsOnAttribute { get; }
        public IXamlType DataTypeAttribute { get; }
        public IXamlType InheritDataTypeFromItemsAttribute { get; }
        public IXamlType InheritDataTypeFromAttribute { get; }
        public IXamlType MarkupExtensionOptionAttribute { get; }
        public IXamlType MarkupExtensionDefaultOptionAttribute { get; }
        public IXamlType ControlTemplateScopeAttribute { get; }
        public IXamlType OldPresentationListAttribute { get; }
        public IXamlType OldPresentationList { get; }
        public IXamlType OnExtensionType { get; }
        public IXamlType UnsetValueType { get; }
        public IXamlType StyledElement { get; }
        public IXamlType NameScope { get; }
        public IXamlMethod NameScopeSetNameScope { get; }
        public IXamlType INameScope { get; }
        public IXamlMethod INameScopeRegister { get; }
        public IXamlMethod INameScopeComplete { get; }
        public IXamlType IPropertyInfo { get; }
        public IXamlType ClrPropertyInfo { get; }
        public IXamlType IPropertyInfoT { get; }
        public IXamlType ClrPropertyInfoT { get; }
        public IXamlType IPropertyAccessor { get; }
        public IXamlType PropertyInfoAccessorFactory { get; }
        public IXamlType CompiledBinding { get; }
        public IXamlType CompiledBindingPathBuilder { get; }
        public IXamlType CompiledBindingPath { get; }
        public IXamlType CompiledBindingExtension { get; }

        public IXamlType ResolveByNameExtension { get; }

        public IXamlType DataTemplate { get; }
        public IXamlType IDataTemplate { get; }
        public IXamlType ITemplateOfControl { get; }
        public IXamlType Control { get; }
        public IXamlType ContentControl { get; }
        public IXamlType ItemsControl { get; }
        public IXamlType ReflectionBindingExtension { get; }

        public IXamlType RelativeSource { get; }
        public IXamlType UInt { get; }
        public IXamlType Int { get; }
        public IXamlType Long { get; }
        public IXamlType Uri { get; }
        public IXamlType TaskOfT { get; }
        public IXamlType IDictionaryT { get; }
        public IXamlType WeakReferenceOfT { get; }
        public IXamlType IObservableOfT { get; }
        public IXamlType FontFamily { get; }
        public IXamlConstructor FontFamilyConstructorUriName { get; }
        public IXamlType Thickness { get; }
        public IXamlConstructor ThicknessFullConstructor { get; }
        public IXamlType ThemeVariant { get; }
        public IXamlType Point { get; }
        public IXamlConstructor PointFullConstructor { get; }
        public IXamlType Vector { get; }
        public IXamlConstructor VectorFullConstructor { get; }
        public IXamlType Size { get; }
        public IXamlConstructor SizeFullConstructor { get; }
        public IXamlType Matrix { get; }
        public IXamlConstructor MatrixFullConstructor { get; }
        public IXamlType CornerRadius { get; }
        public IXamlConstructor CornerRadiusFullConstructor { get; }
        public IXamlType RelativeUnit { get; }
        public IXamlType RelativePoint { get; }
        public IXamlConstructor RelativePointFullConstructor { get; }
        public IXamlType GridLength { get; }
        public IXamlConstructor GridLengthConstructorValueType { get; }
        public IXamlType Color { get; }
        public IXamlType StandardCursorType { get; }
        public IXamlType Cursor { get; }
        public IXamlConstructor CursorTypeConstructor { get; }
        public IXamlType RowDefinition { get; }
        public IXamlType RowDefinitions { get; }
        public IXamlType ColumnDefinition { get; }
        public IXamlType ColumnDefinitions { get; }
        public IXamlType Classes { get; }
        public IXamlMethod ClassesBindMethod { get; }
        public IXamlProperty StyledElementClassesProperty { get; }
        public IXamlType IBrush { get; }
        public IXamlType ImmutableSolidColorBrush { get; }
        public IXamlConstructor ImmutableSolidColorBrushConstructorColor { get; }
        public IXamlType TypeUtilities { get; }
        public IXamlType TextDecorationCollection { get; }
        public IXamlType TextDecorations { get; }
        public IXamlType TextTrimming { get; }
        public IXamlType SetterBase { get; }
        public IXamlType Setter { get; }
        public IXamlType IStyle { get; }
        public IXamlType StyleInclude { get; }
        public IXamlType ResourceInclude { get; }
        public IXamlType MergeResourceInclude { get; }
        public IXamlType IResourceDictionary { get; }
        public IXamlType ResourceDictionary { get; }
        public IXamlMethod ResourceDictionaryDeferredAdd { get; }
        public IXamlMethod ResourceDictionaryNotSharedDeferredAdd { get; }
        public IXamlMethod ResourceDictionaryEnsureCapacity { get; }
        public IXamlMethod ResourceDictionaryGetCount { get; }
        public IXamlType IThemeVariantProvider { get; }
        public IXamlType UriKind { get; }
        public IXamlConstructor UriConstructor { get; }
        public IXamlType Style { get; }
        public IXamlType Container { get; }
        public IXamlType Styles { get; }
        public IXamlType StyleQueries { get; }
        public IXamlType Selectors { get; }
        public IXamlType ControlTheme { get; }
        public IXamlType WindowTransparencyLevel { get; }
        public IXamlType IReadOnlyListOfT { get; }
        public IXamlType ControlTemplate { get; }
        public IXamlType EventHandlerT {  get; }
        public IXamlMethod GetClassProperty { get; }
        public IXamlConstructor XamlSourceInfoConstructor { get; }
        public IXamlMethod XamlSourceInfoSetter { get; }
        public IXamlMethod XamlSourceInfoDictionarySetter { get; }

        sealed internal class InteractivityWellKnownTypes
        {
            public IXamlType Interactive { get; }
            public IXamlType RoutedEvent { get; }
            public IXamlType RoutedEventArgs { get; }
            public IXamlType RoutedEventHandler { get; }
            public IXamlMethod AddHandler { get; }
            public IXamlMethod AddHandlerT { get; }

            [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrCornerstoneAssembly)]
            internal InteractivityWellKnownTypes(IXamlTypeSystem typeSystem, XamlTypeWellKnownTypes wellKnownTypes)
            {
                Interactive = typeSystem.GetType("Cornerstone.Presentation.Interactivity.Interactive");
                RoutedEvent = typeSystem.GetType("Cornerstone.Presentation.Interactivity.RoutedEvent");
                RoutedEventArgs = typeSystem.GetType("Cornerstone.Presentation.Interactivity.RoutedEventArgs");
                var eventHanlderT = typeSystem.GetType("System.EventHandler`1");
                RoutedEventHandler = eventHanlderT.MakeGenericType(RoutedEventArgs);
                AddHandler = Interactive.GetMethod(m => m.IsPublic
                    && !m.IsStatic
                    && m.Name == "AddHandler"
                    && m.Parameters.Count == 4
                    && m.Parameters[0].Equals(RoutedEvent)
                    && m.Parameters[1].Equals(wellKnownTypes.Delegate)
                    && m.Parameters[2].IsEnum
                    && m.Parameters[3].Equals(wellKnownTypes.Boolean)
                    );
                AddHandlerT = Interactive.GetMethod(m => m.IsPublic
                    && !m.IsStatic
                    && m.Name == "AddHandler"
                    && m.Parameters.Count == 4
                    && RoutedEvent.IsAssignableFrom(m.Parameters[0])
                    && m.Parameters[0].GenericArguments.Count == 1 // This is specific this case  workaround to check is generic method
                    && wellKnownTypes.Delegate.IsAssignableFrom(m.Parameters[1])
                    && m.Parameters[2].IsEnum
                    && m.Parameters[3].Equals(wellKnownTypes.Boolean)
                );

            }
        }

        public InteractivityWellKnownTypes Interactivity { get; }

        [UnconditionalSuppressMessage("Trimming", "IL2122", Justification = TrimmingMessages.TypesInCoreOrCornerstoneAssembly)]
        public CornerstoneXamlIlWellKnownTypes(IXamlTypeSystem typeSystem)
        {
            RuntimeHelpers = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime.XamlIlRuntimeHelpers");

            XamlIlTypes = typeSystem.WellKnownTypes;
            PresentationObject = typeSystem.GetType("Cornerstone.Presentation.PresentationObject");
            PresentationObjectExtensions = typeSystem.GetType("Cornerstone.Presentation.PresentationObjectExtensions");
            PresentationProperty = typeSystem.GetType("Cornerstone.Presentation.PresentationProperty");
            PresentationPropertyT = typeSystem.GetType("Cornerstone.Presentation.PresentationProperty`1");
            StyledPropertyT = typeSystem.GetType("Cornerstone.Presentation.StyledProperty`1");
            CornerstoneAttachedPropertyT = typeSystem.GetType("Cornerstone.Presentation.AttachedProperty`1");
            BindingPriority = typeSystem.GetType("Cornerstone.Presentation.Data.BindingPriority");
            PresentationObjectSetStyledPropertyValue = PresentationObject
                .GetMethod(m => m.IsPublic && !m.IsStatic && m.Name == "SetValue"
                                 && m.Parameters.Count == 3
                                 && m.Parameters[0].Name == "StyledProperty`1"
                                 && m.Parameters[2].Equals(BindingPriority));
            BindingBase = typeSystem.GetType("Cornerstone.Presentation.Data.BindingBase");
            BindingExpressionBase = typeSystem.GetType("Cornerstone.Presentation.Data.BindingExpressionBase");
            MultiBinding = typeSystem.GetType("Cornerstone.Presentation.Data.MultiBinding");
            IDisposable = typeSystem.GetType("System.IDisposable");
            ICommand = typeSystem.GetType("System.Windows.Input.ICommand");
            Transitions = typeSystem.GetType("Cornerstone.Presentation.Animation.Transitions");
            AssignBindingAttribute = typeSystem.GetType("Cornerstone.Presentation.Data.AssignBindingAttribute");
            DependsOnAttribute = typeSystem.GetType("Cornerstone.Presentation.Metadata.DependsOnAttribute");
            DataTypeAttribute = typeSystem.GetType("Cornerstone.Presentation.Metadata.DataTypeAttribute");
            InheritDataTypeFromItemsAttribute = typeSystem.GetType("Cornerstone.Presentation.Metadata.InheritDataTypeFromItemsAttribute");
            InheritDataTypeFromAttribute = typeSystem.GetType("Cornerstone.Presentation.Metadata.InheritDataTypeFromAttribute");
            MarkupExtensionOptionAttribute = typeSystem.GetType("Cornerstone.Presentation.Metadata.MarkupExtensionOptionAttribute");
            MarkupExtensionDefaultOptionAttribute = typeSystem.GetType("Cornerstone.Presentation.Metadata.MarkupExtensionDefaultOptionAttribute");
            ControlTemplateScopeAttribute = typeSystem.GetType("Cornerstone.Presentation.Metadata.ControlTemplateScopeAttribute");
            OldPresentationListAttribute = typeSystem.GetType("Cornerstone.Presentation.Metadata.OldPresentationListAttribute");
            OldPresentationList = typeSystem.GetType("Cornerstone.Presentation.Collections.OldPresentationList`1");
            OnExtensionType = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.On");
            PresentationObjectBindMethod = PresentationObject.GetMethod("Bind", BindingExpressionBase, false, PresentationProperty, BindingBase);
            UnsetValueType = typeSystem.GetType("Cornerstone.Presentation.UnsetValueType");
            StyledElement = typeSystem.GetType("Cornerstone.Presentation.StyledElement");
            INameScope = typeSystem.GetType("Cornerstone.Presentation.Controls.Naming.INameScope");
            INameScopeRegister = INameScope.GetMethod(
                new FindMethodMethodSignature("Register", XamlIlTypes.Void,
                     XamlIlTypes.String, XamlIlTypes.Object)
                {
                    IsStatic = false,
                    DeclaringOnly = true,
                    IsExactMatch = true
                });
            INameScopeComplete = INameScope.GetMethod(
                new FindMethodMethodSignature("Complete", XamlIlTypes.Void)
                {
                    IsStatic = false,
                    DeclaringOnly = true,
                    IsExactMatch = true
                });
            NameScope = typeSystem.GetType("Cornerstone.Presentation.Controls.Naming.NameScope");
            NameScopeSetNameScope = NameScope.GetMethod(new FindMethodMethodSignature("SetNameScope",
                XamlIlTypes.Void, StyledElement, INameScope)
            { IsStatic = true });
            PresentationObjectSetValueMethod = PresentationObject.GetMethod("SetValue", IDisposable,
                false, PresentationProperty, XamlIlTypes.Object, BindingPriority);
            IPropertyInfo = typeSystem.GetType("Cornerstone.Presentation.Data.Core.IPropertyInfo");
            ClrPropertyInfo = typeSystem.GetType("Cornerstone.Presentation.Data.Core.ClrPropertyInfo");
            IPropertyInfoT = typeSystem.GetType("Cornerstone.Presentation.Data.Core.IPropertyInfo`2");
            ClrPropertyInfoT = typeSystem.GetType("Cornerstone.Presentation.Data.Core.ClrPropertyInfo`2");
            IPropertyAccessor = typeSystem.GetType("Cornerstone.Presentation.Data.Core.Plugins.IPropertyAccessor");
            PropertyInfoAccessorFactory = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.CompiledBindings.PropertyInfoAccessorFactory");
            CompiledBinding = typeSystem.GetType("Cornerstone.Presentation.Data.CompiledBinding");
            CompiledBindingPathBuilder = typeSystem.GetType("Cornerstone.Presentation.Data.CompiledBindingPathBuilder");
            CompiledBindingPath = typeSystem.GetType("Cornerstone.Presentation.Data.CompiledBindingPath");
            CompiledBindingExtension = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.CompiledBindingExtension");
            ResolveByNameExtension = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.ResolveByNameExtension");
            DataTemplate = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.Templates.DataTemplate");
            IDataTemplate = typeSystem.GetType("Cornerstone.Presentation.Controls.Templates.IDataTemplate");
            Control = typeSystem.GetType("Cornerstone.Presentation.Controls.Control");
            ContentControl = typeSystem.GetType("Cornerstone.Presentation.Controls.ContentControl");
            ITemplateOfControl = typeSystem.GetType("Cornerstone.Presentation.Controls.Templates.ITemplate`1").MakeGenericType(Control);
            ItemsControl = typeSystem.GetType("Cornerstone.Presentation.Controls.ItemsControl");
            ReflectionBindingExtension = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.ReflectionBindingExtension");
            RelativeSource = typeSystem.GetType("Cornerstone.Presentation.Data.RelativeSource");
            UInt = typeSystem.GetType("System.UInt32");
            Int = typeSystem.GetType("System.Int32");
            Long = typeSystem.GetType("System.Int64");
            Uri = typeSystem.GetType("System.Uri");
            TaskOfT = typeSystem.GetType("System.Threading.Tasks.Task`1");
            IDictionaryT = typeSystem.GetType("System.Collections.Generic.IDictionary`2");
            WeakReferenceOfT = typeSystem.GetType("System.WeakReference`1");
            IObservableOfT = typeSystem.GetType("System.IObservable`1");
            FontFamily = typeSystem.GetType("Cornerstone.Presentation.Media.FontFamily");
            FontFamilyConstructorUriName = FontFamily.GetConstructor(new List<IXamlType> { Uri, XamlIlTypes.String });
            ThemeVariant = typeSystem.GetType("Cornerstone.Presentation.Styling.ThemeVariant");
            WindowTransparencyLevel = typeSystem.GetType("Cornerstone.Presentation.Controls.Chrome.WindowTransparencyLevel");

            (IXamlType, IXamlConstructor) GetNumericTypeInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string name, IXamlType componentType, int componentCount)
            {
                var type = typeSystem.GetType(name);
                var ctor = type.GetConstructor(Enumerable.Range(0, componentCount).Select(_ => componentType).ToList());

                return (type, ctor);
            }

            (Thickness, ThicknessFullConstructor) = GetNumericTypeInfo("Cornerstone.Presentation.Thickness", XamlIlTypes.Double, 4);
            (Point, PointFullConstructor) = GetNumericTypeInfo("Cornerstone.Presentation.Point", XamlIlTypes.Double, 2);
            (Vector, VectorFullConstructor) = GetNumericTypeInfo("Cornerstone.Presentation.Vector", XamlIlTypes.Double, 2);
            (Size, SizeFullConstructor) = GetNumericTypeInfo("Cornerstone.Presentation.Size", XamlIlTypes.Double, 2);
            (Matrix, MatrixFullConstructor) = GetNumericTypeInfo("Cornerstone.Presentation.Matrix", XamlIlTypes.Double, 6);
            (CornerRadius, CornerRadiusFullConstructor) = GetNumericTypeInfo("Cornerstone.Presentation.CornerRadius", XamlIlTypes.Double, 4);

            RelativeUnit = typeSystem.GetType("Cornerstone.Presentation.RelativeUnit");
            RelativePoint = typeSystem.GetType("Cornerstone.Presentation.RelativePoint");
            RelativePointFullConstructor = RelativePoint.GetConstructor(new List<IXamlType> { XamlIlTypes.Double, XamlIlTypes.Double, RelativeUnit });

            GridLength = typeSystem.GetType("Cornerstone.Presentation.Controls.Layout.GridLength");
            GridLengthConstructorValueType = GridLength.GetConstructor(new List<IXamlType> { XamlIlTypes.Double, typeSystem.GetType("Cornerstone.Presentation.Controls.Layout.GridUnitType") });
            Color = typeSystem.GetType("Cornerstone.Presentation.Media.Color");
            StandardCursorType = typeSystem.GetType("Cornerstone.Presentation.Input.StandardCursorType");
            Cursor = typeSystem.GetType("Cornerstone.Presentation.Input.Cursor");
            CursorTypeConstructor = Cursor.GetConstructor(new List<IXamlType> { StandardCursorType });
            ColumnDefinition = typeSystem.GetType("Cornerstone.Presentation.Controls.Layout.ColumnDefinition");
            ColumnDefinitions = typeSystem.GetType("Cornerstone.Presentation.Controls.Layout.ColumnDefinitions");
            RowDefinition = typeSystem.GetType("Cornerstone.Presentation.Controls.Layout.RowDefinition");
            RowDefinitions = typeSystem.GetType("Cornerstone.Presentation.Controls.Layout.RowDefinitions");
            Classes = typeSystem.GetType("Cornerstone.Presentation.Controls.StyleClasses.Classes");
            StyledElementClassesProperty =
                StyledElement.Properties.First(x => x.Name == "Classes" && x.PropertyType.Equals(Classes));
            ClassesBindMethod = typeSystem.GetType("Cornerstone.Presentation.StyledElementExtensions")
                .GetMethod("BindClass", IDisposable, false, StyledElement,
                typeSystem.WellKnownTypes.String,
                BindingBase, typeSystem.WellKnownTypes.Object);

            IBrush = typeSystem.GetType("Cornerstone.Presentation.Media.IBrush");
            ImmutableSolidColorBrush = typeSystem.GetType("Cornerstone.Presentation.Media.Immutable.ImmutableSolidColorBrush");
            ImmutableSolidColorBrushConstructorColor = ImmutableSolidColorBrush.GetConstructor(new List<IXamlType> { UInt });
            TypeUtilities = typeSystem.GetType("Cornerstone.Presentation.Utilities.TypeUtilities");
            TextDecorationCollection = typeSystem.GetType("Cornerstone.Presentation.Media.TextDecorationCollection");
            TextDecorations = typeSystem.GetType("Cornerstone.Presentation.Media.TextDecorations");
            TextTrimming = typeSystem.GetType("Cornerstone.Presentation.Media.TextTrimming");
            SetterBase = typeSystem.GetType("Cornerstone.Presentation.Styling.SetterBase");
            Setter = typeSystem.GetType("Cornerstone.Presentation.Styling.Setter");
            IStyle = typeSystem.GetType("Cornerstone.Presentation.Styling.IStyle");
            StyleInclude = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.Styling.StyleInclude");
            ResourceInclude = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.Styling.ResourceInclude");
            MergeResourceInclude = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.Styling.MergeResourceInclude");
            IResourceDictionary = typeSystem.GetType("Cornerstone.Presentation.Controls.Resources.IResourceDictionary");
            ResourceDictionary = typeSystem.GetType("Cornerstone.Presentation.Controls.Resources.ResourceDictionary");
            ResourceDictionaryDeferredAdd = ResourceDictionary.GetMethod("AddDeferred", XamlIlTypes.Void, true, XamlIlTypes.Object,
                typeSystem.GetType("Cornerstone.Presentation.Controls.Templates.IDeferredContent"));
            ResourceDictionaryNotSharedDeferredAdd = ResourceDictionary.GetMethod("AddNotSharedDeferred", XamlIlTypes.Void, true, XamlIlTypes.Object,
                typeSystem.GetType("Cornerstone.Presentation.Controls.Templates.IDeferredContent"));

            ResourceDictionaryEnsureCapacity = ResourceDictionary.GetMethod("EnsureCapacity", XamlIlTypes.Void, true, XamlIlTypes.Int32);
            ResourceDictionaryGetCount = ResourceDictionary.GetMethod("get_Count", XamlIlTypes.Int32, true);
            IThemeVariantProvider = typeSystem.GetType("Cornerstone.Presentation.Controls.Theming.IThemeVariantProvider");
            UriKind = typeSystem.GetType("System.UriKind");
            UriConstructor = Uri.GetConstructor(new List<IXamlType>() { typeSystem.WellKnownTypes.String, UriKind });
            Style = typeSystem.GetType("Cornerstone.Presentation.Styling.Style");
            Container = typeSystem.GetType("Cornerstone.Presentation.Styling.ContainerQuery");
            Styles = typeSystem.GetType("Cornerstone.Presentation.Styling.Styles");
            StyleQueries = typeSystem.GetType("Cornerstone.Presentation.Styling.StyleQueries");
            Selectors = typeSystem.GetType("Cornerstone.Presentation.Styling.Selectors");
            ControlTheme = typeSystem.GetType("Cornerstone.Presentation.Styling.ControlTheme");
            ControlTemplate = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.Templates.ControlTemplate");
            IReadOnlyListOfT = typeSystem.GetType("System.Collections.Generic.IReadOnlyList`1");
            EventHandlerT = typeSystem.GetType("System.EventHandler`1");
            Interactivity = new InteractivityWellKnownTypes(typeSystem, typeSystem.WellKnownTypes);

            GetClassProperty = typeSystem.GetType("Cornerstone.Presentation.StyledElementExtensions")
                .GetMethod(name: "GetClassProperty",
                returnType: PresentationProperty,
                allowDowncast:false,
                typeSystem.WellKnownTypes.String
                );

            var xamlSourceInfo = typeSystem.GetType("Cornerstone.Presentation.Markup.Xaml.XamlSourceInfo");
            XamlSourceInfoConstructor = xamlSourceInfo.GetConstructor([
                XamlIlTypes.Int32, XamlIlTypes.Int32, XamlIlTypes.String
            ]);
            XamlSourceInfoSetter =
                xamlSourceInfo.GetMethod("SetXamlSourceInfo", XamlIlTypes.Void, false, XamlIlTypes.Object, xamlSourceInfo);
            XamlSourceInfoDictionarySetter =
                xamlSourceInfo.GetMethod("SetXamlSourceInfo", XamlIlTypes.Void, false, IResourceDictionary, XamlIlTypes.Object, xamlSourceInfo);
        }
    }

    static class CornerstoneXamlIlWellKnownTypesExtensions
    {
        public static CornerstoneXamlIlWellKnownTypes GetPresentationTypes(this TransformerConfiguration cfg)
            => cfg.GetExtra<CornerstoneXamlIlWellKnownTypes>();

        public static CornerstoneXamlIlWellKnownTypes GetPresentationTypes(this AstTransformationContext ctx)
            => ctx.Configuration.GetPresentationTypes();

        public static CornerstoneXamlIlWellKnownTypes GetPresentationTypes(this XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> ctx)
            => ctx.Configuration.GetPresentationTypes();

        public static CornerstoneXamlIlWellKnownTypes GetPresentationTypes(this AstGroupTransformationContext ctx)
            => ctx.Configuration.GetPresentationTypes();
    }
}
