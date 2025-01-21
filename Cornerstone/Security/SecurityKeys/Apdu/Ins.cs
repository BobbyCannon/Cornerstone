namespace Cornerstone.Security.SecurityKeys.Apdu;

public enum Ins : byte
{
	GetData = 0xCA,
	LoadKeys = 0x82,
	Authenticate = 0x86,
	Verify = 0x20,
	ReadBinary = 0xB0,
	UpdateBinary = 0xD6
}