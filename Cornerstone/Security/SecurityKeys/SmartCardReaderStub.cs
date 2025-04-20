#region References

using System.Threading.Tasks;
using Cornerstone.Attributes;
using Cornerstone.Collections;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public class SmartCardReaderStub : SmartCardReader
{
	#region Constructors

	/// <inheritdoc />
	[DependencyInjectionConstructor]
	public SmartCardReaderStub(IDispatcher dispatcher) : base(dispatcher)
	{
		AvailableReaders = new ReadOnlySpeedyList<SelectionOption<string>>([]);
	}

	#endregion

	#region Properties

	public override ReadOnlySpeedyList<SelectionOption<string>> AvailableReaders { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override byte[] Transmit(byte[] command)
	{
		return [];
	}

	#endregion
}