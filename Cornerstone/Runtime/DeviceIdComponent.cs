namespace Cornerstone.Runtime;

/// <summary>
/// An implementation of <see cref="IDeviceIdComponent" /> that uses either a specified value
/// or the result of a specified function as its component value.
/// </summary>
public class DeviceIdComponent : IDeviceIdComponent
{
	#region Fields

	private string _cachedValue;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="DeviceIdComponent" /> class.
	/// </summary>
	public DeviceIdComponent() : this(null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DeviceIdComponent" /> class.
	/// </summary>
	/// <param name="value"> The component value. </param>
	public DeviceIdComponent(string value)
	{
		_cachedValue = value;
	}

	#endregion

	#region Methods

	/// <summary>
	/// Gets the component value.
	/// </summary>
	/// <returns> The component value. </returns>
	public string GetValue()
	{
		return _cachedValue ??= GetComponentValue();
	}

	protected virtual string GetComponentValue()
	{
		return _cachedValue;
	}

	#endregion
}

/// <summary>
/// Represents a component that forms part of a device identifier.
/// </summary>
public interface IDeviceIdComponent
{
	#region Methods

	/// <summary>
	/// Gets the component value.
	/// </summary>
	/// <returns> The component value. </returns>
	string GetValue();

	#endregion
}