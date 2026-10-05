#region References

using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Security.SecurityKeys;

/// <summary>
/// iOS / iPhone not supported
/// https://stackoverflow.com/questions/73150597/read-mifare-classic-tag-with-iphone
/// </summary>
public class MiFareClassicSecurityCard : SecurityCard
{
	#region Constants

	public const int BlocksPerSector = 4;
	public const int BytesPerBlock = 16;
	public const string CardId = "3B-8F-80-01-80-4F-0C-A0-00-00-03-06-03-00-01-00-00-00-00-6A";
	public const int Sectors = 16;
	public const int TotalBlocks = Sectors * BlocksPerSector;
	public const int TotalSize = TotalBlocks * BytesPerBlock;

	#endregion

	#region Fields

	public static readonly byte[] KeyA = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

	#endregion

	#region Constructors

	public MiFareClassicSecurityCard() : this(null)
	{
	}

	public MiFareClassicSecurityCard(IDispatcher dispatcher)
		: base(TotalSize, dispatcher)
	{
	}

	#endregion

	#region Methods

	public override bool IsUnsafeBlock(byte block)
	{
		return InternalIsWritableBlock(block);
	}

	internal static bool InternalIsWritableBlock(byte block)
	{
		// Do not allow writing to block 0 or any sector ending block
		return (block > 0) && (((block + 1) % 4) != 0);
	}

	#endregion
}