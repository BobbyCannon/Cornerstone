#region References

using Avalonia;
using Avalonia.Input;
using Cornerstone.Avalonia.AvaloniaEdit.Editing;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;

/// <summary>
/// Insight window that shows an OverloadViewer.
/// </summary>
public class OverloadInsightWindow : InsightWindow
{
	#region Fields

	private readonly OverloadViewer _overloadViewer = new();

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new OverloadInsightWindow.
	/// </summary>
	public OverloadInsightWindow(TextArea textArea) : base(textArea)
	{
		_overloadViewer.Margin = new Thickness(2, 0, 0, 0);
		Child = _overloadViewer;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets the item provider.
	/// </summary>
	public IOverloadProvider Provider
	{
		get => _overloadViewer.Provider;
		set => _overloadViewer.Provider = value;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (!e.Handled && (Provider != null) && (Provider.Count > 1))
		{
			switch (e.Key)
			{
				case Key.Up:
					e.Handled = true;
					_overloadViewer.ChangeIndex(-1);
					break;
				case Key.Down:
					e.Handled = true;
					_overloadViewer.ChangeIndex(+1);
					break;
			}
			if (e.Handled)
			{
				// TODO: UpdateLayout();
				UpdatePosition();
			}
		}
	}

	#endregion
}