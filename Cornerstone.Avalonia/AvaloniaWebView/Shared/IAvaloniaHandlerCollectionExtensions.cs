namespace Cornerstone.Avalonia.AvaloniaWebView.Shared;

public static class IAvaloniaHandlerCollectionExtensions
{
	#region Methods

	public static IAvaloniaHandlerCollection AddHandler<TType, TTypeRender>(this IAvaloniaHandlerCollection handlersCollection)
	{
		return handlersCollection;
	}

	#endregion
}