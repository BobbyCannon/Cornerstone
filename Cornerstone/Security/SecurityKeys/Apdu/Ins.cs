namespace Cornerstone.Security.SecurityKeys.Apdu;

public enum Ins : byte
{
	GetData = 0xCA,
	LoadKeys = 0x82,
	Authenticate = 0x86,
	Verify = 0x20,
	ReadBytes = 0xB0,
	WriteBytes = 0xD6
}