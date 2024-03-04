#pragma warning disable 1591

namespace Cornerstone.Protocols.Osc;

public interface IOscArgument
{
	#region Methods

	char GetOscBinaryType();

	string GetOscStringType();

	byte[] GetOscValueBytes();

	string GetOscValueString();

	void ParseOscValue(byte[] value, ref int index);

	void ParseOscValue(string value);

	#endregion
}