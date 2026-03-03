#region References

using Avalonia.Collections;
using Cornerstone.Presentation;

#endregion

namespace Avalonia.Diagnostics.ViewModels;

public abstract class EventTreeNodeBase : ViewModel
{
	#region Fields

	public bool _updateChildren = true;
	public bool _updateParent = true;
	private bool? _isEnabled = false;
	private bool _isExpanded;
	private bool _isVisible;

	#endregion

	#region Constructors

	protected EventTreeNodeBase(EventTreeNodeBase parent, string text)
	{
		Parent = parent;
		Text = text;
		IsVisible = true;
	}

	#endregion

	#region Properties

	public IAvaloniaReadOnlyList<EventTreeNodeBase> Children { get; protected set; }

	public virtual bool? IsEnabled
	{
		get => _isEnabled;
		set => SetProperty(ref _isEnabled, value);
	}

	public bool IsExpanded
	{
		get => _isExpanded;
		set => SetProperty(ref _isExpanded, value);
	}

	public bool IsVisible
	{
		get => _isVisible;
		set => SetProperty(ref _isVisible, value);
	}

	public EventTreeNodeBase Parent { get; }

	public string Text { get; }

	#endregion

	#region Methods

	public void UpdateChecked()
	{
		IsEnabled = GetValue();

		bool? GetValue()
		{
			if (Children == null)
			{
				return false;
			}

			bool? value = false;

			for (var i = 0; i < Children.Count; i++)
			{
				if (i == 0)
				{
					value = Children[i].IsEnabled;
					continue;
				}

				if (value != Children[i].IsEnabled)
				{
					value = null;
					break;
				}
			}

			return value;
		}
	}

	#endregion
}