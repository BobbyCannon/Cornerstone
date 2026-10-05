using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Markup.Xaml.Templates
{
    public class DataTemplate : IRecyclingDataTemplate, ITypedDataTemplate
    {
        [DataType]
        public Type? DataType { get; set; }

        [Content]
        [TemplateContent]
        public object? Content { get; set; }

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

        public Control? Build(object? data) => Build(data, null);

        public Control? Build(object? data, Control? existing)
        {
            return existing ?? TemplateContent.Load(Content)?.Result;
        }
    }
}
