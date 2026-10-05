using Cornerstone.Presentation.Controls.Naming;

namespace Cornerstone.Presentation.Controls.Templates
{
    public interface ITemplateResult
    {
        public object? Result { get; }
        public INameScope NameScope { get; }
    }
}
