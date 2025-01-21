#region References

using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public class UnknownSecurityCard : SecurityCard
{
	#region Constructors

	public UnknownSecurityCard() : this(null)
	{
	}

	/// <inheritdoc />
	public UnknownSecurityCard(IDispatcher dispatcher) : base(0, dispatcher)
	{
	}

	#endregion
}