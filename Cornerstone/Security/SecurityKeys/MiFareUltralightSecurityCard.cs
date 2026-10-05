#region References

using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Security.SecurityKeys;

public class MiFareUltralightSecurityCard : SecurityCard
{
	#region Constants

	public const int BytesPerPage = 4;
	public const string CardId = "3B-8F-80-01-80-4F-0C-A0-00-00-03-06-03-00-03-00-00-00-00-68";
	public const int TotalPages = 16;
	public const int TotalSize = 64;

	#endregion

	#region Constructors

	public MiFareUltralightSecurityCard(IDispatcher dispatcher) 
		: base(TotalSize, dispatcher)
	{
	}

	#endregion
}