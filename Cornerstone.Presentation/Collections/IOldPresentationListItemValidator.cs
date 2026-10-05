namespace Cornerstone.Presentation.Collections;

internal interface IOldPresentationListItemValidator<T>
{
    void Validate(T item);
}
