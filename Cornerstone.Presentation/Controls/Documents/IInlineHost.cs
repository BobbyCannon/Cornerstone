using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.LogicalTree;

namespace Cornerstone.Presentation.Controls.Documents
{
    internal interface IInlineHost : ILogical
    {
        void Invalidate();

        IOldPresentationList<Visual> VisualChildren { get; }
    }
}
