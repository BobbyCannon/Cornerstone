#region References

using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public class SecurityCardReaderStub : SecurityCardReader
{
	#region Constructors

	/// <inheritdoc />
	[DependencyInjectionConstructor]
	public SecurityCardReaderStub(IDispatcher dispatcher) : base(dispatcher)
	{
		IsAvailable = false;
		IsListening = false;
	}

	#endregion

	#region Methods

	public override void ReadCard()
	{
	}

	/// <inheritdoc />
	public override void StartListening()
	{
		IsListening = true;
	}

	/// <inheritdoc />
	public override void StopListening()
	{
		IsListening = false;
	}

	public override void WriteCard()
	{
	}

	#endregion
}