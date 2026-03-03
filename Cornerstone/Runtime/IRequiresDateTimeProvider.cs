namespace Cornerstone.Runtime;

public interface IRequiresDateTimeProvider
{
	#region Methods

	void UpdateDateTimeProvider(IDateTimeProvider dateTimeProvider);

	#endregion
}