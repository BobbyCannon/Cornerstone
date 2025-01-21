#region References

using Cornerstone.Attributes;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public class SmartCardReaderStub : SmartCardReader
{
	#region Constructors

	/// <inheritdoc />
	[DependencyInjectionConstructor]
	public SmartCardReaderStub(WeakEventManager weakEventManager, IDispatcher dispatcher) : base(weakEventManager, dispatcher)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override byte[] Transmit(byte[] command)
	{
		return [];
	}

	#endregion
}