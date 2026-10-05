#region References

using WmiLight;

#endregion

namespace Cornerstone.Agent.Hardware;

internal sealed class WmiLightObjectAdapter : IWmiPropertySource
{
	#region Fields

	private readonly WmiObject _inner;

	#endregion

	#region Constructors

	public WmiLightObjectAdapter(WmiObject inner)
	{
		_inner = inner;
	}

	#endregion

	#region Properties

	public object this[string propertyName] => _inner[propertyName];

	#endregion

	#region Methods

	public WmiObject GetWmiObject()
	{
		return _inner;
	}

	#endregion
}