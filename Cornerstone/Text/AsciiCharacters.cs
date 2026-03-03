#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Text;

public static class AsciiCharacters
{
	#region Constants

	public const byte Acknowledge = 0x06;
	public const char AcknowledgeGlyph = '\u2406';
	public const byte Backspace = 0x08;
	public const char BackspaceGlyph = '\u2408';
	public const byte Bell = 0x07;
	public const char BellGlyph = '\u2407';
	public const byte Cancel = 0x18;
	public const char CancelGlyph = '\u2418';
	public const byte CarriageReturn = 0x0D;
	public const char CarriageReturnGlyph = '\u240D';
	public const byte DataLinkEscape = 0x10;
	public const char DataLinkEscapeGlyph = '\u2410';
	public const byte Delete = 0x7F;
	public const char DeleteGlyph = '\u2421';
	public const byte DeviceControl1 = 0x11;
	public const char DeviceControl1Glyph = '\u2411';
	public const byte DeviceControl2 = 0x12;
	public const char DeviceControl2Glyph = '\u2412';
	public const byte DeviceControl3 = 0x13;
	public const char DeviceControl3Glyph = '\u2413';
	public const byte DeviceControl4 = 0x14;
	public const char DeviceControl4Glyph = '\u2414';
	public const byte EndOfMedium = 0x19;
	public const char EndOfMediumGlyph = '\u2419';
	public const byte EndOfText = 0x03;
	public const char EndOfTextGlyph = '\u2403';
	public const byte EndOfTransmission = 0x04;
	public const byte EndOfTransmissionBlock = 0x17;
	public const char EndOfTransmissionBlockGlyph = '\u2417';
	public const char EndOfTransmissionGlyph = '\u2404';
	public const byte Enquiry = 0x05;
	public const char EnquiryGlyph = '\u2405';
	public const byte Escape = 0x1B;
	public const char EscapeGlyph = '\u241B';
	public const byte FileSeparator = 0x1C;
	public const char FileSeparatorGlyph = '\u241C';
	public const byte FormFeed = 0x0C;
	public const char FormFeedGlyph = '\u240C';
	public const byte GroupSeparator = 0x1D;
	public const char GroupSeparatorGlyph = '\u241D';
	public const byte HorizontalTab = 0x09;
	public const char HorizontalTabGlyph = '\u2409';
	public const byte LineFeed = 0x0A;
	public const char LineFeedGlyph = '\u240A';
	public const byte NegativeAcknowledge = 0x15;
	public const char NegativeAcknowledgeGlyph = '\u2415';

	// ASCII character constants as byte values
	public const byte Null = 0x00;

	// ASCII glyph constants
	public const char NullGlyph = '\u2400';
	public const byte RecordSeparator = 0x1E;
	public const char RecordSeparatorGlyph = '\u241E';
	public const byte ShiftIn = 0x0F;
	public const char ShiftInGlyph = '\u240F';
	public const byte ShiftOut = 0x0E;
	public const char ShiftOutGlyph = '\u240E';
	public const byte Space = 0x20;
	public const char SpaceGlyph = '\u2420';
	public const byte StartOfHeading = 0x01;
	public const char StartOfHeadingGlyph = '\u2401';
	public const byte StartOfText = 0x02;
	public const char StartOfTextGlyph = '\u2402';
	public const byte Substitute = 0x1A;
	public const char SubstituteGlyph = '\u241A';
	public const byte SynchronousIdle = 0x16;
	public const char SynchronousIdleGlyph = '\u2416';
	public const byte UnitSeparator = 0x1F;
	public const char UnitSeparatorGlyph = '\u241F';
	public const byte VerticalTab = 0x0B;
	public const char VerticalTabGlyph = '\u240B';

	#endregion

	#region Fields

	public static readonly IReadOnlyDictionary<AsciiTable, char> Glyphs;

	#endregion

	#region Constructors

	static AsciiCharacters()
	{
		Glyphs = new Dictionary<AsciiTable, char>
		{
			{ AsciiTable.Null, NullGlyph },
			{ AsciiTable.StartOfHeading, StartOfHeadingGlyph },
			{ AsciiTable.StartOfText, StartOfTextGlyph },
			{ AsciiTable.EndOfText, EndOfTextGlyph },
			{ AsciiTable.EndOfTransmission, EndOfTransmissionGlyph },
			{ AsciiTable.Enquiry, EnquiryGlyph },
			{ AsciiTable.Acknowledge, AcknowledgeGlyph },
			{ AsciiTable.Bell, BellGlyph },
			{ AsciiTable.Backspace, BackspaceGlyph },
			{ AsciiTable.HorizontalTab, HorizontalTabGlyph },
			{ AsciiTable.LineFeed, LineFeedGlyph },
			{ AsciiTable.VerticalTab, VerticalTabGlyph },
			{ AsciiTable.FormFeed, FormFeedGlyph },
			{ AsciiTable.CarriageReturn, CarriageReturnGlyph },
			{ AsciiTable.ShiftOut, ShiftOutGlyph },
			{ AsciiTable.ShiftIn, ShiftInGlyph },
			{ AsciiTable.DataLinkEscape, DataLinkEscapeGlyph },
			{ AsciiTable.DeviceControl1, DeviceControl1Glyph },
			{ AsciiTable.DeviceControl2, DeviceControl2Glyph },
			{ AsciiTable.DeviceControl3, DeviceControl3Glyph },
			{ AsciiTable.DeviceControl4, DeviceControl4Glyph },
			{ AsciiTable.NegativeAcknowledge, NegativeAcknowledgeGlyph },
			{ AsciiTable.SynchronousIdle, SynchronousIdleGlyph },
			{ AsciiTable.EndOfTransmissionBlock, EndOfTransmissionBlockGlyph },
			{ AsciiTable.Cancel, CancelGlyph },
			{ AsciiTable.EndOfMedium, EndOfMediumGlyph },
			{ AsciiTable.Substitute, SubstituteGlyph },
			{ AsciiTable.Escape, EscapeGlyph },
			{ AsciiTable.FileSeparator, FileSeparatorGlyph },
			{ AsciiTable.GroupSeparator, GroupSeparatorGlyph },
			{ AsciiTable.RecordSeparator, RecordSeparatorGlyph },
			{ AsciiTable.UnitSeparator, UnitSeparatorGlyph },
			{ AsciiTable.Space, SpaceGlyph },
			{ AsciiTable.Delete, DeleteGlyph }
		};
	}

	#endregion

	#region Methods

	public static char GetGlyph(this AsciiTable asciiValue)
	{
		return Glyphs.TryGetValue(asciiValue, out var glyph) ? glyph : '.';
	}

	#endregion
}