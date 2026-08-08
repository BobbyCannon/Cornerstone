#region References

using System.ComponentModel;

#endregion

namespace Cornerstone.Internal.WeakEvents;

internal class PropertyChangedWeakEventListener<T, T2>
	: WeakEventListener<T, T2, PropertyChangedEventArgs>
	where T : class, INotifyPropertyChanged
	where T2 : class
{
	#region Constructors

	public PropertyChangedWeakEventListener(T source, T2 destination, PropertyChangedEventHandler handler)
		: base(source, typeof(INotifyPropertyChanged), nameof(source.PropertyChanged), destination, handler.Method)
	{
	}

	#endregion
}