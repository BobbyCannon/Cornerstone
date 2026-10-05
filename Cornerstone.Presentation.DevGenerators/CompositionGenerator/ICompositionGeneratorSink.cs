namespace Cornerstone.Presentation.SourceGenerator.CompositionGenerator;

public interface ICompositionGeneratorSink
{
    void AddSource(string name, string code);
}