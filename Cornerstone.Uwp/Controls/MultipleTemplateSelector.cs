#region References

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Uwp.Controls;

[ContentProperty(Name = nameof(Matches))]
public class MultipleTemplateSelector : DataTemplateSelector
{
	#region Constructors

	public MultipleTemplateSelector()
	{
		Matches = new SpeedyList<TemplateMatch>();
	}

	#endregion

	#region Properties

	public SpeedyList<TemplateMatch> Matches { get; set; }

	#endregion

	#region Methods

	protected override DataTemplate SelectTemplateCore(object item)
	{
		if (item == null)
		{
			return null;
		}

		return Matches.FirstOrDefault(m => m.TargetType == item.GetType().Name)?.Template;
	}

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
	{
		return SelectTemplateCore(item);
	}

	#endregion
}