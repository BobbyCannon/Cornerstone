using Microsoft.CodeAnalysis;

namespace Cornerstone.Presentation.SourceGenerator.CompositionGenerator;

class RoslynCompositionGeneratorSink : ICompositionGeneratorSink
{
    private readonly SourceProductionContext _ctx;

    public RoslynCompositionGeneratorSink(SourceProductionContext ctx)
    {
        _ctx = ctx;
    }

    public void AddSource(string name, string code) => _ctx.AddSource(name, code);
}