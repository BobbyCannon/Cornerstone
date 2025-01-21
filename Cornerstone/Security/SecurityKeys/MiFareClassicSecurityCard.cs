#region References

using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public class MiFareClassicSecurityCard : SecurityCard
{
	#region Constructors

	public MiFareClassicSecurityCard() : this(null)
	{
	}

	public MiFareClassicSecurityCard(IDispatcher dispatcher)
		: base(MiFareClassicSecurityCardReader.TotalSize, dispatcher)
	{
	}

	#endregion
}