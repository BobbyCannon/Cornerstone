﻿#region References

using System.Collections.Generic;
using Cornerstone.Extensions;
using Cornerstone.Protocols;
using Cornerstone.Protocols.Osc;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols;

[TestClass]
public class ExtensionTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CalculateCrc16()
	{
		var actual = "123456789"u8.ToArray().CalculateCrc16(CrcType.Kermit);
		actual.ToString("X4").Dump();
		AreEqual(0x2189, actual);

		actual = "123456789"u8.ToArray().CalculateCrc16(CrcType.Xmodem);
		actual.ToString("X4").Dump();
		AreEqual(0x31C3, actual);
	}

	[TestMethod]
	public void EscapeString()
	{
		var items = GetItems();

		foreach (var item in items)
		{
			item.Key.Escape().Dump();
			AreEqual(item.Key, item.Value.Escape());
		}

		// One way escapes
		items = new List<(string, string)>
		{
			("\\'", "'"),
			("John\\\'s Tavern", "John's Tavern")
		};

		foreach (var item in items)
		{
			item.Key.Escape().Dump();
			AreEqual(item.Key, item.Value.Escape());
		}
	}

	[TestMethod]
	public void UnescapeString()
	{
		var items = GetItems();

		foreach (var item in items)
		{
			(item.Key + " / " + item.Key.Unescape() + " : " + item.Value + " / " + item.Value.Escape()).Dump();
			AreEqual(item.Value, item.Key.Unescape());
		}

		"\r\nSpecial Cases\r\n".Dump();

		// One way un-escapes
		items = new List<(string, string)>
		{
			("\\u0000", "\u0000"),
			("\\u00bc", "\u00BC"),
			("\\'", "\'")
		};

		foreach (var item in items)
		{
			(item.Key + " / " + item.Key.Unescape() + " : " + item.Value + " / " + item.Value.Escape()).Dump();
			AreEqual(item.Value, item.Key.Unescape());
		}
	}

	private static List<(string Key, string Value)> GetItems()
	{
		return new List<(string, string)>
		{
			("\\0", "\0"),
			("\\a", "\a"),
			("\\b", "\b"),
			("\\f", "\f"),
			("\\n", "\n"),
			("\\r", "\r"),
			("\\t", "\t"),
			("\\v", "\v"),
			("\\'", "\'"),
			("\\\"", "\""),
			("\\u0001", "\u0001"),
			("\\uFFFF", "\uFFFF"),
			("\\u1234", "\x1234"),
			("\\u0001", "\x1"),
			("\\u0012", "\x12"),
			("\\u0123", "\x123"),
			("\\u000E", "\xE"),
			("\\u00EC", "\xEC"),
			("\\u0DAF", "\xDAF"),
			("\\u000F", "\xF"),
			("\\u00EF", "\xEF"),
			("\\u0CBA", "\xCBA"),
			("\\uDCBA", "\xDCBA"),
			("\\uDCBAF", "\xDCBAF"),
			("John\\'s Tavern", "John\'s Tavern")
		};
	}

	#endregion
}