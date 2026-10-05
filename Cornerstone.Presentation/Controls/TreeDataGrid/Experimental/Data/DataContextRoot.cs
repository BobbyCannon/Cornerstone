#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.TreeDataGrid.Experimental.Data.Core;

#endregion

namespace Cornerstone.Presentation.Controls.TreeDataGrid.Experimental.Data;

internal class DataContextRoot<T> : SingleSubscriberObservableBase<T>
	where T : class
{
	#region Fields

	private readonly StyledElement _source;

	#endregion

	#region Constructors

	public DataContextRoot(StyledElement source)
	{
		_source = source;
	}

	#endregion

	#region Methods

	protected override void Subscribed()
	{
		_source.PropertyChanged += PropertyChanged;
		PublishValue();
	}

	protected override void Unsubscribed()
	{
		_source.PropertyChanged -= PropertyChanged;
	}

	private void PropertyChanged(object sender, PresentationPropertyChangedEventArgs e)
	{
		if (e.Property == StyledElement.DataContextProperty)
		{
			PublishValue();
		}
	}

	private void PublishValue()
	{
		if (_source.DataContext is null)
		{
			PublishNext(null);
		}
		else if (_source.DataContext is T value)
		{
			PublishNext(value);
		}

		// TODO: Log DataContext is unexpected type.
	}

	#endregion
}