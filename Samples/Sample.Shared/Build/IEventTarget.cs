namespace Sample.Shared.Build;

public interface IEventTarget
{
	#region Methods

	void SubscribeEvents();

	void UnsubscribeAll();

	void UnsubscribeEvents();

	#endregion
}