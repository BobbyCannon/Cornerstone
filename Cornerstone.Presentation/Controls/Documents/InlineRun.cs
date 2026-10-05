using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Text;

namespace Cornerstone.Presentation.Controls.Documents
{
    internal class EmbeddedControlRun : DrawableTextRun
    {
        public EmbeddedControlRun(Control control, TextRunProperties properties)
        {
            Control = control;
            Properties = properties;
        }

        public Control Control { get; }

        public override TextRunProperties? Properties { get; }

        public override Size Size => Control.DesiredSize;

        public override double Baseline
        {
            get
            {
                double baseline = Size.Height;
                double baselineOffsetValue = Control.GetValue<double>(TextBlock.BaselineOffsetProperty);

                if (!MathUtilities.IsZero(baselineOffsetValue))
                {
                    baseline = baselineOffsetValue;
                }

                return baseline;
            }
        }

        public override void Draw(DrawingContext drawingContext, Point origin)
        {
            //noop            
        }
    }
}
