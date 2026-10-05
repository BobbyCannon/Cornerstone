using System;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.VisualTree;

namespace Cornerstone.Presentation.Media
{
    /// <summary>
    /// Translates (moves) an <see cref="Visual"/>.
    /// </summary>
    public sealed class TranslateTransform : Transform
    {
        /// <summary>
        /// Defines the <see cref="X"/> property.
        /// </summary>
        public static readonly StyledProperty<double> XProperty =
            PresentationProperty.Register<TranslateTransform, double>(nameof(X));

        /// <summary>
        /// Defines the <see cref="Y"/> property.
        /// </summary>
        public static readonly StyledProperty<double> YProperty =
            PresentationProperty.Register<TranslateTransform, double>(nameof(Y));

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslateTransform"/> class.
        /// </summary>
        public TranslateTransform()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslateTransform"/> class.
        /// </summary>
        /// <param name="x">Gets the horizontal offset of the translate.</param>
        /// <param name="y">Gets the vertical offset of the translate.</param>
        public TranslateTransform(double x, double y)
            : this()
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets the horizontal offset of the translate.
        /// </summary>
        public double X
        {
            get { return GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        /// <summary>
        /// Gets the vertical offset of the translate.
        /// </summary>
        public double Y
        {
            get { return GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        /// <summary>
        /// Gets the transform's <see cref="Matrix"/>.
        /// </summary>
        public override Matrix Value => Matrix.CreateTranslation(X, Y);
        
        protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == XProperty || change.Property == YProperty)
            {
                RaiseChanged();
            }
        }
    }
}
