using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Controls
{
    /// <summary>
    /// A panel that displays child controls at arbitrary locations.
    /// </summary>
    /// <remarks>
    /// Unlike other <see cref="Panel"/> implementations, the <see cref="Canvas"/> doesn't lay out
    /// its children in any particular layout. Instead, the positioning of each child control is
    /// defined by the <code>Canvas.Left</code>, <code>Canvas.Top</code>, <code>Canvas.Right</code>
    /// and <code>Canvas.Bottom</code> attached properties.
    /// </remarks>
    public class Canvas : Panel, INavigableContainer
    {
        /// <summary>
        /// Defines the Left attached property.
        /// </summary>
        public static readonly AttachedProperty<double> LeftProperty =
            PresentationProperty.RegisterAttached<Canvas, Control, double>("Left", double.NaN);

        /// <summary>
        /// Defines the Top attached property.
        /// </summary>
        public static readonly AttachedProperty<double> TopProperty =
            PresentationProperty.RegisterAttached<Canvas, Control, double>("Top", double.NaN);

        /// <summary>
        /// Defines the Right attached property.
        /// </summary>
        public static readonly AttachedProperty<double> RightProperty =
            PresentationProperty.RegisterAttached<Canvas, Control, double>("Right", double.NaN);

        /// <summary>
        /// Defines the Bottom attached property.
        /// </summary>
        public static readonly AttachedProperty<double> BottomProperty =
            PresentationProperty.RegisterAttached<Canvas, Control, double>("Bottom", double.NaN);

        /// <summary>
        /// Initializes static members of the <see cref="Canvas"/> class.
        /// </summary>
        static Canvas()
        {
            ClipToBoundsProperty.OverrideDefaultValue<Canvas>(false);
            AffectsParentArrange<Canvas>(LeftProperty, TopProperty, RightProperty, BottomProperty);
        }

        /// <summary>
        /// Gets the value of the Left attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's left coordinate.</returns>
        public static double GetLeft(PresentationObject element)
        {
            return element.GetValue(LeftProperty);
        }

        /// <summary>
        /// Sets the value of the Left attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The left value.</param>
        public static void SetLeft(PresentationObject element, double value)
        {
            element.SetValue(LeftProperty, value);
        }

        /// <summary>
        /// Gets the value of the Top attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's top coordinate.</returns>
        public static double GetTop(PresentationObject element)
        {
            return element.GetValue(TopProperty);
        }

        /// <summary>
        /// Sets the value of the Top attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The top value.</param>
        public static void SetTop(PresentationObject element, double value)
        {
            element.SetValue(TopProperty, value);
        }

        /// <summary>
        /// Gets the value of the Right attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's right coordinate.</returns>
        public static double GetRight(PresentationObject element)
        {
            return element.GetValue(RightProperty);
        }

        /// <summary>
        /// Sets the value of the Right attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The right value.</param>
        public static void SetRight(PresentationObject element, double value)
        {
            element.SetValue(RightProperty, value);
        }

        /// <summary>
        /// Gets the value of the Bottom attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's bottom coordinate.</returns>
        public static double GetBottom(PresentationObject element)
        {
            return element.GetValue(BottomProperty);
        }

        /// <summary>
        /// Sets the value of the Bottom attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The bottom value.</param>
        public static void SetBottom(PresentationObject element, double value)
        {
            element.SetValue(BottomProperty, value);
        }

        /// <summary>
        /// Gets the next control in the specified direction.
        /// </summary>
        /// <param name="direction">The movement direction.</param>
        /// <param name="from">The control from which movement begins.</param>
        /// <param name="wrap">Whether to wrap around when the first or last item is reached.</param>
        /// <returns>The control.</returns>
        IInputElement? INavigableContainer.GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            // TODO: Implement this
            return null;
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            foreach (Control child in Children)
            {
                child.Measure(availableSize);
            }

            return new Size();
        }

        /// <summary>
        /// Arranges a single child.
        /// </summary>
        /// <param name="child">The child to arrange.</param>
        /// <param name="finalSize">The size allocated to the canvas.</param>
        protected virtual void ArrangeChild(Control child, Size finalSize)
        {
            double x = 0.0;
            double y = 0.0;
            double elementLeft = GetLeft(child);

            if (!double.IsNaN(elementLeft))
            {
                x = elementLeft;
            }
            else
            {
                // Arrange with right.
                double elementRight = GetRight(child);
                if (!double.IsNaN(elementRight))
                {
                    x = finalSize.Width - child.DesiredSize.Width - elementRight;
                }
            }

            double elementTop = GetTop(child);
            if (!double.IsNaN(elementTop))
            {
                y = elementTop;
            }
            else
            {
                double elementBottom = GetBottom(child);
                if (!double.IsNaN(elementBottom))
                {
                    y = finalSize.Height - child.DesiredSize.Height - elementBottom;
                }
            }

            child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
        }

        /// <summary>
        /// Arranges the control's children.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (Control child in Children)
            {
                ArrangeChild(child, finalSize);
            }

            return finalSize;
        }
    }
}
