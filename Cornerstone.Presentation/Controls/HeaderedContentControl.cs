using System.Text;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Presenters;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Primitives;

namespace Cornerstone.Presentation.Controls
{
    /// <summary>
    /// A <see cref="ContentControl"/> with a header.
    /// </summary>
    [TemplatePart("PART_HeaderPresenter", typeof(ContentPresenter))]
    public class HeaderedContentControl : ContentControl, IHeadered
    {
        /// <summary>
        /// Defines the <see cref="Header"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> HeaderProperty =
            PresentationProperty.Register<HeaderedContentControl, object?>(nameof(Header));

        /// <summary>
        /// Defines the <see cref="HeaderTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
            PresentationProperty.Register<HeaderedContentControl, IDataTemplate?>(nameof(HeaderTemplate));

        /// <summary>
        /// Initializes static members of the <see cref="ContentControl"/> class.
        /// </summary>
        static HeaderedContentControl()
        {
            HeaderProperty.Changed.AddClassHandler<HeaderedContentControl>((x, e) => x.HeaderChanged(e));
        }

        /// <summary>
        /// Gets or sets the header content.
        /// </summary>
        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets the header presenter from the control's template.
        /// </summary>
        public ContentPresenter? HeaderPresenter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the data template used to display the header content of the control.
        /// </summary>
        public IDataTemplate? HeaderTemplate
        {
            get => GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <inheritdoc/>
        protected override bool RegisterContentPresenter(ContentPresenter presenter)
        {
            var result = base.RegisterContentPresenter(presenter);

            if (presenter.Name == "PART_HeaderPresenter")
            {
                HeaderPresenter = presenter;
                result = true;
            }

            return result;
        }

        private void HeaderChanged(PresentationPropertyChangedEventArgs e)
        {
            if (e.OldValue is ILogical oldChild)
            {
                LogicalChildren.Remove(oldChild);
            }

            if (e.NewValue is ILogical newChild)
            {
                LogicalChildren.Add(newChild);
            }
        }

        internal override void BuildDebugDisplay(StringBuilder builder, bool includeContent)
        {
            base.BuildDebugDisplay(builder, includeContent);

            if (includeContent)
            {
                DebugDisplayHelper.AppendOptionalValue(builder, nameof(Header), Header, true);
            }
        }
    }
}
