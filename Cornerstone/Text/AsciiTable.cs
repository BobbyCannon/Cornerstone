#region References

using System.ComponentModel.DataAnnotations;

#endregion

// ReSharper disable InconsistentNaming

namespace Cornerstone.Text;

public enum AsciiTable : byte
{
	[Display(Name = "Null", ShortName = "NUL")]
	Null = 0,

	[Display(Name = "Start of Heading", ShortName = "SOH")]
	StartOfHeading = 1,

	[Display(Name = "Start of Text", ShortName = "STX")]
	StartOfText = 2,

	[Display(Name = "End of Text", ShortName = "ETX")]
	EndOfText = 3,

	[Display(Name = "End of Transmission", ShortName = "EOT")]
	EndOfTransmission = 4,

	[Display(Name = "Enquiry", ShortName = "ENQ")]
	Enquiry = 5,

	[Display(Name = "Acknowledge", ShortName = "ACK")]
	Acknowledge = 6,

	[Display(Name = "Bell", ShortName = "BEL")]
	Bell = 7,

	[Display(Name = "Backspace", ShortName = "BS")]
	Backspace = 8,

	[Display(Name = "Horizontal Tab", ShortName = "TAB")]
	HorizontalTab = 9,

	[Display(Name = "Line Feed", ShortName = "LF")]
	LineFeed = 10,

	[Display(Name = "Vertical Tab", ShortName = "VT")]
	VerticalTab = 11,

	[Display(Name = "Form Feed", ShortName = "FF")]
	FormFeed = 12,

	[Display(Name = "Carriage Return", ShortName = "CR")]
	CarriageReturn = 13,

	[Display(Name = "Shift Out", ShortName = "SO")]
	ShiftOut = 14,

	[Display(Name = "Shift In", ShortName = "SI")]
	ShiftIn = 15,

	[Display(Name = "Data Link Escape", ShortName = "DLE")]
	DataLinkEscape = 16,

	[Display(Name = "Device Control 1", ShortName = "DC1")]
	DeviceControl1 = 17,

	[Display(Name = "Device Control 2", ShortName = "DC2")]
	DeviceControl2 = 18,

	[Display(Name = "Device Control 3", ShortName = "DC3")]
	DeviceControl3 = 19,

	[Display(Name = "Device Control 4", ShortName = "DC4")]
	DeviceControl4 = 20,

	[Display(Name = "Negative Acknowledge", ShortName = "NAK")]
	NegativeAcknowledge = 21,

	[Display(Name = "Synchronous Idle", ShortName = "SYN")]
	SynchronousIdle = 22,

	[Display(Name = "End of Transmission Block", ShortName = "ETB")]
	EndOfTransmissionBlock = 23,

	[Display(Name = "Cancel", ShortName = "CAN")]
	Cancel = 24,

	[Display(Name = "End of Medium", ShortName = "EM")]
	EndOfMedium = 25,

	[Display(Name = "Substitute", ShortName = "SUB")]
	Substitute = 26,

	[Display(Name = "Escape", ShortName = "ESC")]
	Escape = 27,

	[Display(Name = "File Separator", ShortName = "FS")]
	FileSeparator = 28,

	[Display(Name = "Group Separator", ShortName = "GS")]
	GroupSeparator = 29,

	[Display(Name = "Record Separator", ShortName = "RS")]
	RecordSeparator = 30,

	[Display(Name = "Unit Separator", ShortName = "US")]
	UnitSeparator = 31,

	[Display(Name = "Space", ShortName = "SP")]
	Space = 32,

	[Display(Name = "Exclamation Mark", ShortName = "!")]
	Exclamation = 33,

	[Display(Name = "Double Quote", ShortName = "\"")]
	DoubleQuote = 34,

	[Display(Name = "Number Sign", ShortName = "#")]
	Hash = 35,

	[Display(Name = "Dollar Sign", ShortName = "$")]
	Dollar = 36,

	[Display(Name = "Percent Sign", ShortName = "%")]
	Percent = 37,

	[Display(Name = "Ampersand", ShortName = "&")]
	Ampersand = 38,

	[Display(Name = "Single Quote", ShortName = "'")]
	SingleQuote = 39,

	[Display(Name = "Left Parenthesis", ShortName = "(")]
	LeftParen = 40,

	[Display(Name = "Right Parenthesis", ShortName = ")")]
	RightParen = 41,

	[Display(Name = "Asterisk", ShortName = "*")]
	Asterisk = 42,

	[Display(Name = "Plus Sign", ShortName = "+")]
	Plus = 43,

	[Display(Name = "Comma", ShortName = ",")]
	Comma = 44,

	[Display(Name = "Hyphen", ShortName = "-")]
	Minus = 45,

	[Display(Name = "Period", ShortName = ".")]
	Period = 46,

	[Display(Name = "Forward Slash", ShortName = "/")]
	Slash = 47,

	[Display(Name = "Digit Zero", ShortName = "0")]
	Zero = 48,

	[Display(Name = "Digit One", ShortName = "1")]
	One = 49,

	[Display(Name = "Digit Two", ShortName = "2")]
	Two = 50,

	[Display(Name = "Digit Three", ShortName = "3")]
	Three = 51,

	[Display(Name = "Digit Four", ShortName = "4")]
	Four = 52,

	[Display(Name = "Digit Five", ShortName = "5")]
	Five = 53,

	[Display(Name = "Digit Six", ShortName = "6")]
	Six = 54,

	[Display(Name = "Digit Seven", ShortName = "7")]
	Seven = 55,

	[Display(Name = "Digit Eight", ShortName = "8")]
	Eight = 56,

	[Display(Name = "Digit Nine", ShortName = "9")]
	Nine = 57,

	[Display(Name = "Colon", ShortName = ":")]
	Colon = 58,

	[Display(Name = "Semicolon", ShortName = ";")]
	Semicolon = 59,

	[Display(Name = "Less Than", ShortName = "<")]
	LessThan = 60,

	[Display(Name = "Equals Sign", ShortName = "=")]
	Equals = 61,

	[Display(Name = "Greater Than", ShortName = ">")]
	GreaterThan = 62,

	[Display(Name = "Question Mark", ShortName = "?")]
	Question = 63,

	[Display(Name = "At Sign", ShortName = "@")]
	At = 64,

	[Display(Name = "Uppercase A", ShortName = "A")]
	A = 65,

	[Display(Name = "Uppercase B", ShortName = "B")]
	B = 66,

	[Display(Name = "Uppercase C", ShortName = "C")]
	C = 67,

	[Display(Name = "Uppercase D", ShortName = "D")]
	D = 68,

	[Display(Name = "Uppercase E", ShortName = "E")]
	E = 69,

	[Display(Name = "Uppercase F", ShortName = "F")]
	F = 70,

	[Display(Name = "Uppercase G", ShortName = "G")]
	G = 71,

	[Display(Name = "Uppercase H", ShortName = "H")]
	H = 72,

	[Display(Name = "Uppercase I", ShortName = "I")]
	I = 73,

	[Display(Name = "Uppercase J", ShortName = "J")]
	J = 74,

	[Display(Name = "Uppercase K", ShortName = "K")]
	K = 75,

	[Display(Name = "Uppercase L", ShortName = "L")]
	L = 76,

	[Display(Name = "Uppercase M", ShortName = "M")]
	M = 77,

	[Display(Name = "Uppercase N", ShortName = "N")]
	N = 78,

	[Display(Name = "Uppercase O", ShortName = "O")]
	O = 79,

	[Display(Name = "Uppercase P", ShortName = "P")]
	P = 80,

	[Display(Name = "Uppercase Q", ShortName = "Q")]
	Q = 81,

	[Display(Name = "Uppercase R", ShortName = "R")]
	R = 82,

	[Display(Name = "Uppercase S", ShortName = "S")]
	S = 83,

	[Display(Name = "Uppercase T", ShortName = "T")]
	T = 84,

	[Display(Name = "Uppercase U", ShortName = "U")]
	U = 85,

	[Display(Name = "Uppercase V", ShortName = "V")]
	V = 86,

	[Display(Name = "Uppercase W", ShortName = "W")]
	W = 87,

	[Display(Name = "Uppercase X", ShortName = "X")]
	X = 88,

	[Display(Name = "Uppercase Y", ShortName = "Y")]
	Y = 89,

	[Display(Name = "Uppercase Z", ShortName = "Z")]
	Z = 90,

	[Display(Name = "Left Square Bracket", ShortName = "[")]
	LeftBracket = 91,

	[Display(Name = "Backslash", ShortName = "\\")]
	Backslash = 92,

	[Display(Name = "Right Square Bracket", ShortName = "]")]
	RightBracket = 93,

	[Display(Name = "Caret", ShortName = "^")]
	Caret = 94,

	[Display(Name = "Underscore", ShortName = "_")]
	Underscore = 95,

	[Display(Name = "Backtick", ShortName = "`")]
	Backtick = 96,

	[Display(Name = "Lowercase a", ShortName = "a")]
	a = 97,

	[Display(Name = "Lowercase b", ShortName = "b")]
	b = 98,

	[Display(Name = "Lowercase c", ShortName = "c")]
	c = 99,

	[Display(Name = "Lowercase d", ShortName = "d")]
	d = 100,

	[Display(Name = "Lowercase e", ShortName = "e")]
	e = 101,

	[Display(Name = "Lowercase f", ShortName = "f")]
	f = 102,

	[Display(Name = "Lowercase g", ShortName = "g")]
	g = 103,

	[Display(Name = "Lowercase h", ShortName = "h")]
	h = 104,

	[Display(Name = "Lowercase i", ShortName = "i")]
	i = 105,

	[Display(Name = "Lowercase j", ShortName = "j")]
	j = 106,

	[Display(Name = "Lowercase k", ShortName = "k")]
	k = 107,

	[Display(Name = "Lowercase l", ShortName = "l")]
	l = 108,

	[Display(Name = "Lowercase m", ShortName = "m")]
	m = 109,

	[Display(Name = "Lowercase n", ShortName = "n")]
	n = 110,

	[Display(Name = "Lowercase o", ShortName = "o")]
	o = 111,

	[Display(Name = "Lowercase p", ShortName = "p")]
	p = 112,

	[Display(Name = "Lowercase q", ShortName = "q")]
	q = 113,

	[Display(Name = "Lowercase r", ShortName = "r")]
	r = 114,

	[Display(Name = "Lowercase s", ShortName = "s")]
	s = 115,

	[Display(Name = "Lowercase t", ShortName = "t")]
	t = 116,

	[Display(Name = "Lowercase u", ShortName = "u")]
	u = 117,

	[Display(Name = "Lowercase v", ShortName = "v")]
	v = 118,

	[Display(Name = "Lowercase w", ShortName = "w")]
	w = 119,

	[Display(Name = "Lowercase x", ShortName = "x")]
	x = 120,

	[Display(Name = "Lowercase y", ShortName = "y")]
	y = 121,

	[Display(Name = "Lowercase z", ShortName = "z")]
	z = 122,

	[Display(Name = "Left Curly Brace", ShortName = "{")]
	LeftBrace = 123,

	[Display(Name = "Vertical Bar", ShortName = "|")]
	Pipe = 124,

	[Display(Name = "Right Curly Brace", ShortName = "}")]
	RightBrace = 125,

	[Display(Name = "Tilde", ShortName = "~")]
	Tilde = 126,

	[Display(Name = "Delete", ShortName = "DEL")]
	Delete = 127
}