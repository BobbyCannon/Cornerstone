#region References

using Cornerstone.Presentation;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Profiling;

/// <summary>
/// Represents a counter for profiling.
/// </summary>
public class Counter : Bindable
{
	#region Fields

	private int _count;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiate the counter for profiling.
	/// </summary>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	public Counter(IDispatcher dispatcher = null) : base(dispatcher)
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// The current count value;
	/// </summary>
	public int Value => _count;

	#endregion

	#region Methods

	/// <summary>
	/// Decrement the counter by a value or default(1) if not provided.
	/// </summary>
	/// <param name="decrease"> The value to be decremented. </param>
	public void Decrement(int decrease = 1)
	{
		ThreadSafe.Decrement(ref _count, decrease);
		NotifyOfPropertyChanged(nameof(Value));
	}

	/// <summary>
	/// Increment the counter by a value or default(1) if not provided.
	/// </summary>
	/// <param name="increase"> The value to be incremented. </param>
	public void Increment(int increase = 1)
	{
		ThreadSafe.Increment(ref _count, increase);
		NotifyOfPropertyChanged(nameof(Value));
	}

	/// <summary>
	/// Reset the counter to zero.
	/// </summary>
	public void Reset()
	{
		ThreadSafe.Set(ref _count, 0);
		NotifyOfPropertyChanged(nameof(Value));
	}

	#endregion
}