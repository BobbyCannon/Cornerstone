#region References

using Avalonia;
using Avalonia.VisualTree;
using Cornerstone.Avalonia.TreeDataGrid.Experimental.Data.Core;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Experimental.Data;

internal class ParentDataContextRoot<T> : SingleSubscriberObservableBase<T>
	where T : class
{
	#region Fields

	private readonly Visual _source;

	#endregion

	#region Constructors

	public ParentDataContextRoot(Visual source)
	{
		_source = source;
	}

	#endregion

	#region Methods

	protected override void Subscribed()
	{
		_source.PropertyChanged += SourcePropertyChanged;
		StartListeningToDataContext(_source.GetVisualParent());
		PublishValue();
	}

	protected override void Unsubscribed()
	{
		_source.PropertyChanged -= SourcePropertyChanged;
	}

	private void ParentPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property == StyledElement.DataContextProperty)
		{
			PublishValue();
		}
	}

	private void PublishValue()
	{
		var parent = _source.GetVisualParent() as StyledElement;

		if (parent?.DataContext is null)
		{
			PublishNext(null);
		}
		else if (parent.DataContext is T value)
		{
			PublishNext(value);
		}

		// TODO: Log DataContext is unexpected type.
	}

	private void SourcePropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		// TODO: Double check this with @grokys
		if (e.Property == Visual.VisualParentProperty)
		{
			StopListeningToDataContext(_source.GetVisualParent());
			StartListeningToDataContext(_source.GetVisualParent());
			PublishValue();
		}
	}

	private void StartListeningToDataContext(Visual visual)
	{
		if (visual is StyledElement styled)
		{
			styled.PropertyChanged += ParentPropertyChanged;
		}
	}

	private void StopListeningToDataContext(Visual visual)
	{
		if (visual is StyledElement styled)
		{
			styled.PropertyChanged -= ParentPropertyChanged;
		}
	}

	#endregion
}