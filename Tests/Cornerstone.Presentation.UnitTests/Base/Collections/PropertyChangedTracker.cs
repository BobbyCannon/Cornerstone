#region References

using System.Collections.Generic;
using System.ComponentModel;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Collections;

internal class PropertyChangedTracker
{
	#region Constructors

	public PropertyChangedTracker(INotifyPropertyChanged obj)
	{
		Names = [];
		obj.PropertyChanged += PropertyChanged;
	}

	#endregion

	#region Properties

	public List<string> Names { get; }

	#endregion

	#region Methods

	public void Reset()
	{
		Names.Clear();
	}

	private void PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		Names.Add(e.PropertyName);
	}

	#endregion
}