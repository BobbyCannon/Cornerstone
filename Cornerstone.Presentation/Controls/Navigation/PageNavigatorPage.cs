#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Controls.Navigation;

public class PageNavigatorPage
{
	#region Constructors

	public PageNavigatorPage(string title, Control content)
	{
		Title = title ?? string.Empty;
		Content = content;
	}

	#endregion

	#region Properties

	public Control Content { get; }

	public string Title { get; set; }

	#endregion
}