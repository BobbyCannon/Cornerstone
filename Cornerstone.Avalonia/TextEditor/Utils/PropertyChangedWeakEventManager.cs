#region References

using System.ComponentModel;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Utils;

/// <summary>
/// WeakEventManager for INotifyPropertyChanged.PropertyChanged.
/// </summary>
internal sealed class PropertyChangedWeakEventManager : WeakEventManagerBase<PropertyChangedWeakEventManager, INotifyPropertyChanged, PropertyChangedEventHandler, PropertyChangedEventArgs>
{
	#region Methods

	protected override void StartListening(INotifyPropertyChanged source)
	{
		source.PropertyChanged += DeliverEvent;
	}

	protected override void StopListening(INotifyPropertyChanged source)
	{
		source.PropertyChanged -= DeliverEvent;
	}

	#endregion
}