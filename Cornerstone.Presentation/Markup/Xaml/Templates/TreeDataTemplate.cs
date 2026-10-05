using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Markup.Xaml.Templates
{
    public class TreeDataTemplate : ITreeDataTemplate, ITypedDataTemplate
    {
        [DataType]
        public Type? DataType { get; set; }

        [Content]
        [TemplateContent]
        public object? Content { get; set; }

        [AssignBinding]
        public BindingBase? ItemsSource { get; set; }

        public bool Match(object? data)
        {
            if (DataType == null)
            {
                return true;
            }
            else
            {
                return DataType.IsInstanceOfType(data);
            }
        }

        public IDisposable BindChildren(PresentationObject target, PresentationProperty targetProperty, object item)
        {
            return ItemsSource is not null ?
                target.Bind(targetProperty, ItemsSource) :
                Disposable.Empty;
        }

        public Control? Build(object? data)
        {
            var visualTreeForItem = TemplateContent.Load(Content)?.Result;
            if (visualTreeForItem != null)
            {
                visualTreeForItem.DataContext = data;
            }

            return visualTreeForItem;
        }
    }
}
