#region References

using Cornerstone.Keystone.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Keystone;

[TestClass]
public class BusHistoryFilterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void EmptyMatchesAll()
	{
		var filter = BusHistoryFilter.Parse(string.Empty);
		IsTrue(filter.IsMatchAll);
		IsTrue(filter.Matches(Row("NotificationChannel", 0, "ShowMessage", false, string.Empty)));
	}

	[TestMethod]
	public void ChannelContainsCaseInsensitive()
	{
		var filter = BusHistoryFilter.Parse("channel:notification");
		IsTrue(filter.Matches(Row("NotificationChannel", 0, "X", false, string.Empty)));
		IsFalse(filter.Matches(Row("SettingsChannel", 0, "X", false, string.Empty)));
	}

	[TestMethod]
	public void TypeExactAndCommaOr()
	{
		var filter = BusHistoryFilter.Parse("type:0,2");
		IsTrue(filter.Matches(Row("Any", 0, "A", false, string.Empty)));
		IsTrue(filter.Matches(Row("Any", 2, "A", false, string.Empty)));
		IsFalse(filter.Matches(Row("Any", 1, "A", false, string.Empty)));
	}

	[TestMethod]
	public void ChannelAndTypeAnd()
	{
		var filter = BusHistoryFilter.Parse("channel:Notification type:0");
		IsTrue(filter.Matches(Row("NotificationChannel", 0, "ShowMessage", false, string.Empty)));
		IsFalse(filter.Matches(Row("NotificationChannel", 1, "ShowMessage", false, string.Empty)));
		IsFalse(filter.Matches(Row("SettingsChannel", 0, "ShowMessage", false, string.Empty)));
	}

	[TestMethod]
	public void ErrorTrueOnly()
	{
		var filter = BusHistoryFilter.Parse("error:true");
		IsTrue(filter.Matches(Row("Any", 0, "X", true, "boom")));
		IsFalse(filter.Matches(Row("Any", 0, "X", false, string.Empty)));
	}

	[TestMethod]
	public void FreeTextMatchesNameOrError()
	{
		var filter = BusHistoryFilter.Parse("ShowMessage");
		IsTrue(filter.Matches(Row("NotificationChannel", 0, "ShowMessage", false, string.Empty)));
		IsFalse(filter.Matches(Row("NotificationChannel", 0, "Other", false, string.Empty)));
		IsTrue(filter.Matches(Row("Any", 0, "Other", true, "ShowMessage failed")));
	}

	[TestMethod]
	public void InvalidTypeTokenIgnored()
	{
		var filter = BusHistoryFilter.Parse("type:abc");
		IsTrue(filter.IsMatchAll);
	}

	[TestMethod]
	public void MatchesPublishResult()
	{
		var filter = BusHistoryFilter.Parse("channel:Test type:7");
		var ok = new ChannelMessagePublishResult("TestChannel", 7, null, 10, 1, false, string.Empty);
		var bad = new ChannelMessagePublishResult("Other", 7, null, 10, 1, false, string.Empty);
		IsTrue(filter.Matches(ok));
		IsFalse(filter.Matches(bad));
	}

	private static ChannelMessageHistory Row(string channel, int type, string name, bool hadError, string error)
	{
		return new ChannelMessageHistory
		{
			ChannelName = channel,
			Type = type,
			Name = name,
			HadError = hadError,
			ErrorMessage = error,
			Sequence = 1
		};
	}

	#endregion
}
