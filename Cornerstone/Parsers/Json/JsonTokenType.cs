﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cornerstone.Parsers.Json;

public enum JsonTokenType
{
	None = 0,
	CurlyOpen = 1,
	CurlyClose = 2,
	SquaredOpen = 3,
	SquaredClose = 4,
	Colon = 5,
	Comma = 6,
	String = 7,
	NumberFloat = 8,
	NumberInteger = 9,
	NumberUnsignedInteger = 10,
	Boolean = 11,
	Null = 12
}