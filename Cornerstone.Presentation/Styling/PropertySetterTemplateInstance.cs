using System;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.PropertyStore;

namespace Cornerstone.Presentation.Styling
{
    internal class PropertySetterTemplateInstance : IValueEntry, ISetterInstance
    {
        private readonly ITemplate _template;
        private object? _value;

        public PropertySetterTemplateInstance(PresentationProperty property, ITemplate template)
        {
            _template = template;
            Property = property;
        }

        public PresentationProperty Property { get; }

        public bool HasValue() => true;
        public object? GetValue() => _value ??= _template.Build();

        bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
        {
            state = BindingValueType.Value;
            error = null;
            return false;
        }

        void IValueEntry.Unsubscribe() { }
    }
}
