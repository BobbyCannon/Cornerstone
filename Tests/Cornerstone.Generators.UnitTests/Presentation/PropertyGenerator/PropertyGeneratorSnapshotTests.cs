#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Cornerstone.Generators.UnitTests.Presentation.PropertyGenerator.PropertyGeneratorTestHelper;
using Cornerstone.Presentation.Controls.Layout;

namespace Cornerstone.Generators.UnitTests.Presentation.PropertyGenerator;

[TestClass]
public class PropertyGeneratorSnapshotTests
{
    [TestMethod]
    public void StyledBasic() => AssertGeneratedCode("StyledBasic", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty]
            public partial string? Header { get; set; }
        }
        """,
        expectedHintName: "TestNs.MyControl.CornerstoneProperties.g.cs");

    [TestMethod]
    public void StyledConstDefault() => AssertGeneratedCode("StyledConstDefault", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty(DefaultValue = 100)]
            public partial int Width { get; set; }
        }
        """);

    [TestMethod]
    public void StyledConstDefaultNeedsCast() => AssertGeneratedCode("StyledConstDefaultNeedsCast", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty(DefaultValue = 100)]
            public partial double Width { get; set; }

            [GeneratedStyledProperty(DefaultValue = double.NaN)]
            public partial double Height { get; set; }

            [GeneratedStyledProperty(DefaultValue = "")]
            public partial object? Tag { get; set; }
        }
        """);

    [TestMethod]
    public void StyledStaticCtorCoexists() => AssertGeneratedCode("StyledStaticCtorCoexists", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            static MyControl()
            {
                PaddingProperty.OverrideDefaultValue<MyControl>(new Thickness(4));
            }

            [GeneratedStyledProperty]
            public partial Thickness Padding { get; set; }
        }
        """);

    [TestMethod]
    public void StyledInheritsBindingMode() => AssertGeneratedCode("StyledInheritsBindingMode", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty(Inherits = true, DefaultBindingMode = BindingMode.TwoWay)]
            public partial double FontSize { get; set; }
        }
        """);

    [TestMethod]
    public void StyledValidateCoerce() => AssertGeneratedCode("StyledValidateCoerce", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty(ValidateMethodName = nameof(ValidateValue), CoerceMethodName = nameof(CoerceValue), DefaultValue = 0)]
            public partial int Value { get; set; }

            private static bool ValidateValue(int value) => value >= 0;

            private static int CoerceValue(PresentationObject sender, int value) => value > 100 ? 100 : value;
        }
        """);

    [TestMethod]
    public void StyledSharedCallback() => AssertGeneratedCode("StyledSharedCallback", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty(CoerceMethodName = nameof(CoerceValue))]
            public partial int First { get; set; }

            [GeneratedStyledProperty(CoerceMethodName = nameof(CoerceValue))]
            public partial int Second { get; set; }

            private static int CoerceValue(PresentationObject sender, int value) => value;
        }
        """);

    [TestMethod]
    public void StyledInheritedCallback() => AssertGeneratedCode("StyledInheritedCallback", """
        namespace TestNs;

        public class BaseClass : PresentationObject
        {
            protected static bool ValidateAnswer(int value) => value == 42;
        }

        public partial class DerivedClass : BaseClass
        {
            [GeneratedStyledProperty(ValidateMethodName = nameof(ValidateAnswer))]
            public partial int Answer { get; set; }
        }
        """,
        expectedHintName: "TestNs.DerivedClass.CornerstoneProperties.g.cs");

    [TestMethod]
    [Ignore("Generated AddOwner compilation currently errors against PresentationObject.")]
    public void StyledAddOwner() => AssertGeneratedCode("StyledAddOwner", """
        namespace TestNs;

        public class RangeBase : PresentationObject
        {
            public static readonly StyledProperty<double> ValueProperty =
                PresentationProperty.Register<RangeBase, double>(nameof(Value));

            public double Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }
        }

        public partial class MyControl : RangeBase
        {
            [GeneratedStyledProperty(AddOwnerFrom = typeof(RangeBase))]
            public new partial double Value { get; set; }
        }
        """);

    [TestMethod]
    [Ignore("Generated AddOwner compilation currently errors against PresentationObject.")]
    public void StyledAddOwnerOverrides() => AssertGeneratedCode("StyledAddOwnerOverrides", """
        namespace TestNs;

        public class RangeBase : PresentationObject
        {
            public static readonly StyledProperty<double> ValueProperty =
                PresentationProperty.Register<RangeBase, double>(nameof(Value));

            public double Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }
        }

        public partial class MyControl : RangeBase
        {
            [GeneratedStyledProperty(AddOwnerFrom = typeof(RangeBase), DefaultValue = 1.0, CoerceMethodName = nameof(CoerceValue), EnableDataValidation = true)]
            public new partial double Value { get; set; }

            private static double CoerceValue(PresentationObject sender, double value) => value < 0 ? 0 : value;
        }
        """);

    [TestMethod]
    public void StyledAddOwnerFromGeneratedProperty() => AssertGeneratedCode("StyledAddOwnerFromGeneratedProperty", """
        namespace TestNs;

        public partial class RangeBase : PresentationObject
        {
            // The AddOwner source is itself source-generated.
            [GeneratedStyledProperty]
            public partial double Value { get; set; }
        }

        public partial class MyControl : RangeBase
        {
            [GeneratedStyledProperty(AddOwnerFrom = typeof(RangeBase))]
            public new partial double Value { get; set; }
        }
        """,
        expectedHintName: "TestNs.MyControl.CornerstoneProperties.g.cs");

    [TestMethod]
    public void StyledNonPublicSetter() => AssertGeneratedCode("StyledNonPublicSetter", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty]
            public partial bool IsPressed { get; private set; }
        }
        """);

    [TestMethod]
    public void DirectBasic() => AssertGeneratedCode("DirectBasic", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedDirectProperty]
            public partial string Text { get; set; } = "";
        }
        """);

    // On C# 13 there is no field keyword, so direct properties get a named backing field instead.
    // An inline initializer (= expr) is not valid here and is intentionally omitted (see Direct_Basic).
    [TestMethod]
    public void DirectBasicCSharp13() => AssertGeneratedCode("DirectBasicCSharp13", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedDirectProperty]
            public partial string? Text { get; set; }
        }
        """, languageVersion: LanguageVersion.CSharp13);

    [TestMethod]
    public void DirectReadOnlyCSharp13() => AssertGeneratedCode("DirectReadOnlyCSharp13", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedDirectProperty]
            public partial int SelectedIndex { get; private set; }

            public void Select(int index) => SelectedIndex = index;
        }
        """, languageVersion: LanguageVersion.CSharp13);

    [TestMethod]
    public void DirectRefTypeInitializer() => AssertGeneratedCode("DirectRefTypeInitializer", """
        using System.Collections;
        using Cornerstone.Presentation.Collections;

        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedDirectProperty]
            public partial IEnumerable? Items { get; set; } = new PresentationList<object>();
        }
        """);

    [TestMethod]
    public void DirectReadOnly() => AssertGeneratedCode("DirectReadOnly", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedDirectProperty]
            public partial int SelectedIndex { get; private set; } = -1;

            public void Select(int index) => SelectedIndex = index;
        }
        """);

    [TestMethod]
    public void DirectUnset() => AssertGeneratedCode("DirectUnset", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedDirectProperty(UnsetValue = -1)]
            public partial int Count { get; set; } = -1;
        }
        """);

    [TestMethod]
    [Ignore("Generated AddOwner compilation currently errors against PresentationObject.")]
    public void DirectAddOwner() => AssertGeneratedCode("DirectAddOwner", """
        namespace TestNs;

        public class TextBase : PresentationObject
        {
            public static readonly DirectProperty<TextBase, string> TextProperty =
                PresentationProperty.RegisterDirect<TextBase, string>(nameof(Text), static o => o.Text, static (o, v) => o.Text = v);

            private string _text = "";

            public string Text
            {
                get => _text;
                set => SetAndRaise(TextProperty, ref _text, value);
            }
        }

        public partial class MyControl : PresentationObject
        {
            [GeneratedDirectProperty(AddOwnerFrom = typeof(TextBase))]
            public partial string Text { get; set; } = "";
        }
        """);

    [TestMethod]
    public void AttachedBasic() => AssertGeneratedCode("AttachedBasic", """
        namespace TestNs;

        public partial class Grid : PresentationObject
        {
            [GeneratedAttachedProperty]
            public static partial int GetRow(Visual element);
        }
        """);

    [TestMethod]
    public void AttachedDefaultInherits() => AssertGeneratedCode("AttachedDefaultInherits", """
        namespace TestNs;

        public partial class Grid : PresentationObject
        {
            [GeneratedAttachedProperty(DefaultValue = 1)]
            public static partial int GetRowSpan(Visual element);

            [GeneratedAttachedProperty(Inherits = true)]
            public static partial double GetFontSize(Visual element);
        }
        """);

    [TestMethod]
    public void AttachedValidateCoerce() => AssertGeneratedCode("AttachedValidateCoerce", """
        namespace TestNs;

        public partial class Grid : PresentationObject
        {
            [GeneratedAttachedProperty(ValidateMethodName = nameof(ValidateOrder), CoerceMethodName = nameof(CoerceOrder), DefaultValue = 0)]
            public static partial int GetOrder(Visual element);

            private static bool ValidateOrder(int value) => value >= 0;

            private static int CoerceOrder(PresentationObject sender, int value) => value < 0 ? 0 : value;
        }
        """);

    [TestMethod]
    public void AttachedNonPublicAccessors() => AssertGeneratedCode("AttachedNonPublicAccessors", """
        namespace TestNs;

        public partial class Host : PresentationObject
        {
            [GeneratedAttachedProperty]
            internal static partial bool GetIsHosted(Visual element);
        }
        """);

    [TestMethod]
    public void AttachedNullable() => AssertGeneratedCode("AttachedNullable", """
        namespace TestNs;

        public partial class ToolTip : PresentationObject
        {
            [GeneratedAttachedProperty]
            public static partial string? GetTip(Visual element);
        }
        """);

    [TestMethod]
    public void AttachedStaticOwner() => AssertGeneratedCode("AttachedStaticOwner", """
        namespace TestNs;

        public static partial class ScrollHelper
        {
            [GeneratedAttachedProperty(DefaultValue = false)]
            public static partial bool GetIsScrollTarget(Visual element);
        }
        """,
        expectedHintName: "TestNs.ScrollHelper.CornerstoneProperties.g.cs");

    [TestMethod]
    [Ignore("Generated AddOwner compilation currently errors against PresentationObject.")]
    public void AttachedAddOwner() => AssertGeneratedCode("AttachedAddOwner", """
        namespace TestNs;

        public class BasePanel : PresentationObject
        {
            public static readonly AttachedProperty<int> RowProperty =
                PresentationProperty.RegisterAttached<BasePanel, Visual, int>("Row");
        }

        public partial class MyPanel : BasePanel
        {
            [GeneratedAttachedProperty(AddOwnerFrom = typeof(BasePanel), DefaultValue = 2)]
            public static partial int GetRow(Visual element);
        }
        """);

    [TestMethod]
    public void NestedOwner() => AssertGeneratedCode("NestedOwner", """
        namespace TestNs;

        public partial class Outer
        {
            public partial class MyControl : PresentationObject
            {
                [GeneratedStyledProperty]
                public partial string? Header { get; set; }
            }
        }
        """,
        expectedHintName: "TestNs.Outer.MyControl.CornerstoneProperties.g.cs");

    [TestMethod]
    public void GlobalNamespace() => AssertGeneratedCode("GlobalNamespace", """
        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty]
            public partial string? Header { get; set; }
        }
        """,
        expectedHintName: "MyControl.CornerstoneProperties.g.cs");

    [TestMethod]
    public void GenericOwner() => AssertGeneratedCode("GenericOwner", """
        namespace TestNs;

        public partial class MyControl<T> : PresentationObject
            where T : class
        {
            [GeneratedStyledProperty]
            public partial T? Item { get; set; }
        }
        """,
        expectedHintName: "TestNs.MyControl_1.CornerstoneProperties.g.cs");

    [TestMethod]
    public void MultiProperty() => AssertGeneratedCode("MultiProperty", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty]
            public partial bool First { get; set; }

            [GeneratedStyledProperty]
            public partial bool Second { get; set; }

            [GeneratedDirectProperty]
            public partial string Text { get; set; } = "";

            [GeneratedAttachedProperty]
            public static partial int GetOrder(Visual element);
        }
        """);

    [TestMethod]
    public void NullableDisabledStyled() => AssertGeneratedCode("NullableDisabledStyled", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty]
            public partial string Header { get; set; }

            [GeneratedStyledProperty]
            public partial int Width { get; set; }
        }
        """,
        nullableContextOptions: NullableContextOptions.Disable);

    [TestMethod]
    public void NullableDisabledAttached() => AssertGeneratedCode("NullableDisabledAttached", """
        namespace TestNs;

        public partial class MyPanel : PresentationObject
        {
            [GeneratedAttachedProperty]
            public static partial string GetLabel(Visual element);
        }
        """,
        nullableContextOptions: NullableContextOptions.Disable);

    [TestMethod]
    public void NullableDisabledValidateCoerce() => AssertGeneratedCode("NullableDisabledValidateCoerce", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty(ValidateMethodName = nameof(ValidateHeader), CoerceMethodName = nameof(CoerceHeader))]
            public partial string Header { get; set; }

            private static bool ValidateHeader(string value) => true;

            private static string CoerceHeader(PresentationObject sender, string value) => value;
        }
        """,
        nullableContextOptions: NullableContextOptions.Disable);

    [TestMethod]
    public void NullableMixedInlineDirective() => AssertGeneratedCode("NullableMixedInlineDirective", """
        namespace TestNs;

        public partial class MyControl : PresentationObject
        {
            [GeneratedStyledProperty]
            public partial string? Enabled { get; set; }

        #nullable disable
            [GeneratedStyledProperty]
            public partial string Disabled { get; set; }

            [GeneratedAttachedProperty]
            public static partial string GetDisabledAttached(Visual element);
        #nullable restore

            [GeneratedDirectProperty]
            public partial string EnabledAgain { get; set; } = "";
        }
        """);
}
