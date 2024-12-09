namespace Cornerstone.Weaver.Fody.PropertyChanged;

partial class WeaverForPropertyChanged
{
	#region Fields

	public EventArgsCache EventArgsCache;

	#endregion

	#region Methods

	public void InitEventArgsCache()
	{
		EventArgsCache = new(this);
	}

	public void InjectEventArgsCache()
	{
		EventArgsCache.InjectType();
	}

	#endregion
}