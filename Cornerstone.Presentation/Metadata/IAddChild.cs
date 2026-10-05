namespace Cornerstone.Presentation.Metadata
{
    public interface IAddChild
    {
        void AddChild(object child);
    }

    public interface IAddChild<T>
    {
        void AddChild(T child);
    }
}
