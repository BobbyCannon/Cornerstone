using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.Controls.Naming;

namespace Cornerstone.Presentation.Controls
{
    public partial class RelativePanel
    {

        static RelativePanel()
        {
            ClipToBoundsProperty.OverrideDefaultValue<RelativePanel>(true);

            AffectsParentArrange<RelativePanel>(
                AlignLeftWithPanelProperty, AlignLeftWithProperty, LeftOfProperty,
                AlignRightWithPanelProperty, AlignRightWithProperty, RightOfProperty,
                AlignTopWithPanelProperty, AlignTopWithProperty, AboveProperty,
                AlignBottomWithPanelProperty, AlignBottomWithProperty, BelowProperty,
                AlignHorizontalCenterWithPanelProperty, AlignHorizontalCenterWithProperty,
                AlignVerticalCenterWithPanelProperty, AlignVerticalCenterWithProperty);
            
            AffectsParentMeasure<RelativePanel>(
                AlignLeftWithPanelProperty, AlignLeftWithProperty, LeftOfProperty,
                AlignRightWithPanelProperty, AlignRightWithProperty, RightOfProperty,
                AlignTopWithPanelProperty, AlignTopWithProperty, AboveProperty,
                AlignBottomWithPanelProperty, AlignBottomWithProperty, BelowProperty,
                AlignHorizontalCenterWithPanelProperty, AlignHorizontalCenterWithProperty,
                AlignVerticalCenterWithPanelProperty, AlignVerticalCenterWithProperty);
        }

        /// <summary>
        /// Gets the value of the RelativePanel.Above XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.Above XAML attached property value of the specified object.
        /// (The element to position this element above.)
        /// </returns>        
        [ResolveByName]
        public static object GetAbove(PresentationObject obj)
        {
            return (object)obj.GetValue(AboveProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element above.)</param>
        [ResolveByName]
        public static void SetAbove(PresentationObject obj, object value)
        {
            obj.SetValue(AboveProperty, value);
        }


        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AboveProperty"/> XAML attached property.
        /// </summary>        

        public static readonly AttachedProperty<object> AboveProperty =
            PresentationProperty.RegisterAttached<Layoutable, object>("Above", typeof(RelativePanel));


        /// <summary>
        /// Gets the value of the RelativePanel.AlignBottomWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignBottomWithPanel XAML attached property value of the specified
        ///    object. (true to align this element's bottom edge with the panel's bottom edge;
        /// otherwise, false.)
        /// </returns>
        public static bool GetAlignBottomWithPanel(PresentationObject obj)
        {
            return (bool)obj.GetValue(AlignBottomWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        /// The value to set. (true to align this element's bottom edge with the panel's
        /// bottom edge; otherwise, false.)
        /// </param>
        public static void SetAlignBottomWithPanel(PresentationObject obj, bool value)
        {
            obj.SetValue(AlignBottomWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignBottomWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignBottomWithPanelProperty =
            PresentationProperty.RegisterAttached<Layoutable, bool>("AlignBottomWithPanel", typeof(RelativePanel));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignBottomWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignBottomWith XAML attached property value of the specified object.
        /// (The element to align this element's bottom edge with.)
        /// </returns>        
        [ResolveByName]
        public static object GetAlignBottomWith(PresentationObject obj)
        {
            return (object)obj.GetValue(AlignBottomWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's bottom edge with.)</param>
        [ResolveByName]
        public static void SetAlignBottomWith(PresentationObject obj, object value)
        {
            obj.SetValue(AlignBottomWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignBottomWithProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> AlignBottomWithProperty =
            PresentationProperty.RegisterAttached<Layoutable, object>("AlignBottomWith", typeof(RelativePanel));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignHorizontalCenterWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignHorizontalCenterWithPanel XAML attached property value
        /// of the specified object. (true to horizontally center this element in the panel;
        /// otherwise, false.)
        /// </returns>
        public static bool GetAlignHorizontalCenterWithPanel(PresentationObject obj)
        {
            return (bool)obj.GetValue(AlignHorizontalCenterWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        /// The value to set. (true to horizontally center this element in the panel; otherwise,
        /// false.)
        /// </param>
        public static void SetAlignHorizontalCenterWithPanel(PresentationObject obj, bool value)
        {
            obj.SetValue(AlignHorizontalCenterWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignHorizontalCenterWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignHorizontalCenterWithPanelProperty =
            PresentationProperty.RegisterAttached<Layoutable, bool>("AlignHorizontalCenterWithPanel", typeof(RelativePanel), false);

        /// <summary>
        /// Gets the value of the RelativePanel.AlignHorizontalCenterWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignHorizontalCenterWith XAML attached property value of the
        /// specified object. (The element to align this element's horizontal center with.)
        /// </returns>        
        [ResolveByName]
        public static object GetAlignHorizontalCenterWith(PresentationObject obj)
        {
            return (object)obj.GetValue(AlignHorizontalCenterWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's horizontal center with.)</param>
        [ResolveByName]
        public static void SetAlignHorizontalCenterWith(PresentationObject obj, object value)
        {
            obj.SetValue(AlignHorizontalCenterWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignHorizontalCenterWithProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> AlignHorizontalCenterWithProperty =
            PresentationProperty.RegisterAttached<Layoutable, object>("AlignHorizontalCenterWith", typeof(RelativePanel));

        /// <summary>
        /// Gets the value of the RelativePanel.AlignLeftWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignLeftWithPanel XAML attached property value of the specified
        /// object. (true to align this element's left edge with the panel's left edge; otherwise,
        /// false.)
        /// </returns>
        public static bool GetAlignLeftWithPanel(PresentationObject obj)
        {
            return (bool)obj.GetValue(AlignLeftWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        ///  The value to set. (true to align this element's left edge with the panel's left
        ///  edge; otherwise, false.)
        /// </param>
        public static void SetAlignLeftWithPanel(PresentationObject obj, bool value)
        {
            obj.SetValue(AlignLeftWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignLeftWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignLeftWithPanelProperty =
            PresentationProperty.RegisterAttached<Layoutable, bool>("AlignLeftWithPanel", typeof(RelativePanel), false);


        /// <summary>
        /// Gets the value of the RelativePanel.AlignLeftWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignLeftWith XAML attached property value of the specified
        /// object. (The element to align this element's left edge with.)
        /// </returns>        
        [ResolveByName]
        public static object GetAlignLeftWith(PresentationObject obj)
        {
            return (object)obj.GetValue(AlignLeftWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's left edge with.)</param>
        [ResolveByName]
        public static void SetAlignLeftWith(PresentationObject obj, object value)
        {
            obj.SetValue(AlignLeftWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignLeftWithProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> AlignLeftWithProperty =
            PresentationProperty.RegisterAttached<RelativePanel, Layoutable, object>("AlignLeftWith");


        /// <summary>
        /// Gets the value of the RelativePanel.AlignRightWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignRightWithPanel XAML attached property value of the specified
        /// object. (true to align this element's right edge with the panel's right edge;
        /// otherwise, false.)
        /// </returns>
        public static bool GetAlignRightWithPanel(PresentationObject obj)
        {
            return (bool)obj.GetValue(AlignRightWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        /// The value to set. (true to align this element's right edge with the panel's right
        /// edge; otherwise, false.)
        /// </param>
        public static void SetAlignRightWithPanel(PresentationObject obj, bool value)
        {
            obj.SetValue(AlignRightWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignRightWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignRightWithPanelProperty =
            PresentationProperty.RegisterAttached<RelativePanel, Layoutable, bool>("AlignRightWithPanel", false);

        /// <summary>
        /// Gets the value of the RelativePanel.AlignRightWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignRightWith XAML attached property value of the specified
        /// object. (The element to align this element's right edge with.)
        /// </returns>        
        [ResolveByName]
        public static object GetAlignRightWith(PresentationObject obj)
        {
            return (object)obj.GetValue(AlignRightWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignRightWith XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's right edge with.)</param>
        [ResolveByName]
        public static void SetAlignRightWith(PresentationObject obj, object value)
        {
            obj.SetValue(AlignRightWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignRightWithProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> AlignRightWithProperty =
            PresentationProperty.RegisterAttached<RelativePanel, Layoutable, object>("AlignRightWith");

        /// <summary>
        /// Gets the value of the RelativePanel.AlignTopWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignTopWithPanel XAML attached property value of the specified
        /// object. (true to align this element's top edge with the panel's top edge; otherwise,
        /// false.)
        /// </returns>
        public static bool GetAlignTopWithPanel(PresentationObject obj)
        {
            return (bool)obj.GetValue(AlignTopWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignTopWithPanel XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        /// The value to set. (true to align this element's top edge with the panel's top
        /// edge; otherwise, false.)
        /// </param>
        public static void SetAlignTopWithPanel(PresentationObject obj, bool value)
        {
            obj.SetValue(AlignTopWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignTopWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignTopWithPanelProperty =
            PresentationProperty.RegisterAttached<RelativePanel, Layoutable, bool>("AlignTopWithPanel", false);

        /// <summary>
        /// Gets the value of the RelativePanel.AlignTopWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>The value to set. (The element to align this element's top edge with.)</returns>        
        [ResolveByName]
        public static object GetAlignTopWith(PresentationObject obj)
        {
            return (object)obj.GetValue(AlignTopWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignTopWith XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's top edge with.)</param>
        [ResolveByName]
        public static void SetAlignTopWith(PresentationObject obj, object value)
        {
            obj.SetValue(AlignTopWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignTopWithProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> AlignTopWithProperty =
            PresentationProperty.RegisterAttached<RelativePanel, Layoutable, object>("AlignTopWith");

        /// <summary>
        /// Gets the value of the RelativePanel.AlignVerticalCenterWithPanel XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.AlignVerticalCenterWithPanel XAML attached property value of
        /// the specified object. (true to vertically center this element in the panel; otherwise,
        /// false.)
        /// </returns>
        public static bool GetAlignVerticalCenterWithPanel(PresentationObject obj)
        {
            return (bool)obj.GetValue(AlignVerticalCenterWithPanelProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignVerticalCenterWithPanel XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">
        /// The value to set. (true to vertically center this element in the panel; otherwise,
        /// false.)
        /// </param>
        public static void SetAlignVerticalCenterWithPanel(PresentationObject obj, bool value)
        {
            obj.SetValue(AlignVerticalCenterWithPanelProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignVerticalCenterWithPanelProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> AlignVerticalCenterWithPanelProperty =
            PresentationProperty.RegisterAttached<RelativePanel, Layoutable, bool>("AlignVerticalCenterWithPanel", false);

        /// <summary>
        /// Gets the value of the RelativePanel.AlignVerticalCenterWith XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>The value to set. (The element to align this element's vertical center with.)</returns>        
        [ResolveByName]
        public static object GetAlignVerticalCenterWith(PresentationObject obj)
        {
            return (object)obj.GetValue(AlignVerticalCenterWithProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.AlignVerticalCenterWith XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to align this element's horizontal center with.)</param>        
        [ResolveByName]
        public static void SetAlignVerticalCenterWith(PresentationObject obj, object value)
        {
            obj.SetValue(AlignVerticalCenterWithProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.AlignVerticalCenterWithProperty"/> XAML attached property.
        /// </summary>
        public static readonly AttachedProperty<object> AlignVerticalCenterWithProperty =
            PresentationProperty.RegisterAttached<RelativePanel, Layoutable, object>("AlignVerticalCenterWith");

        /// <summary>
        /// Gets the value of the RelativePanel.Below XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.Below XAML attached property value of the specified object.
        /// (The element to position this element below.)                                
        /// </returns>       
        [ResolveByName]
        public static object GetBelow(PresentationObject obj)
        {
            return (object)obj.GetValue(BelowProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.Above XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element below.)</param>
        [ResolveByName]
        public static void SetBelow(PresentationObject obj, object value)
        {
            obj.SetValue(BelowProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.BelowProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> BelowProperty =
            PresentationProperty.RegisterAttached<RelativePanel, Layoutable, object>("Below");

        /// <summary>
        /// Gets the value of the RelativePanel.LeftOf XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.LeftOf XAML attached property value of the specified object.
        /// (The element to position this element to the left of.)                                 
        /// </returns>        
        [ResolveByName]
        public static object GetLeftOf(PresentationObject obj)
        {
            return (object)obj.GetValue(LeftOfProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.LeftOf XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element to the left of.)</param>
        [ResolveByName]
        public static void SetLeftOf(PresentationObject obj, object value)
        {
            obj.SetValue(LeftOfProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.LeftOfProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> LeftOfProperty =
            PresentationProperty.RegisterAttached<RelativePanel, Layoutable, object>("LeftOf");

        /// <summary>
        /// Gets the value of the RelativePanel.RightOf XAML attached property for the target element.
        /// </summary>
        /// <param name="obj">The object from which the property value is read.</param>
        /// <returns>
        /// The RelativePanel.RightOf XAML attached property value of the specified object.
        /// (The element to position this element to the right of.)                                   
        /// </returns>        
        [ResolveByName]
        public static object GetRightOf(PresentationObject obj)
        {
            return (object)obj.GetValue(RightOfProperty);
        }

        /// <summary>
        /// Sets the value of the RelativePanel.RightOf XAML attached property for a target element.
        /// </summary>
        /// <param name="obj">The object to which the property value is written.</param>
        /// <param name="value">The value to set. (The element to position this element to the right of.)</param>
        [ResolveByName]
        public static void SetRightOf(PresentationObject obj, object value)
        {
            obj.SetValue(RightOfProperty, value);
        }

        /// <summary>
        ///  Identifies the <see cref="RelativePanel.RightOfProperty"/> XAML attached property.
        /// </summary>

        public static readonly AttachedProperty<object> RightOfProperty =
            PresentationProperty.RegisterAttached<RelativePanel, Layoutable, object>("RightOf");
    }
}
