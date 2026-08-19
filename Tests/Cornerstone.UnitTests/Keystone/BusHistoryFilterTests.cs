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
		IsTrue(filter.Matches(Row("NotificationChannel", "ShowMessage", "ShowMessage", false, string.Empty)));
	}

	[TestMethod]
	public void ChannelContainsCaseInsensitive()
	{
		var filter = BusHistoryFilter.Parse("channel:notification");
		IsTrue(filter.Matches(Row("NotificationChannel", "X", "X", false, string.Empty)));
		IsFalse(filter.Matches(Row("SettingsChannel", "X", "X", false, string.Empty)));
	}

	[TestMethod]
	public void TypeContainsAndCommaOr()
	{
		var filter = BusHistoryFilter.Parse("type:MessageA,MessageC");
		IsTrue(filter.Matches(Row("Any", "MessageA", "A", false, string.Empty)));
		IsTrue(filter.Matches(Row("Any", "MessageC", "A", false, string.Empty)));
		IsFalse(filter.Matches(Row("Any", "MessageB", "A", false, string.Empty)));
	}

	[TestMethod]
	public void ChannelAndTypeAnd()
	{
		var filter = BusHistoryFilter.Parse("channel:Notification type:Notification");
		IsTrue(filter.Matches(Row("NotificationChannel", "NotificationMessage", "ShowMessage", false, string.Empty)));
		IsFalse(filter.Matches(Row("NotificationChannel", "Other", "ShowMessage", false, string.Empty)));
		IsFalse(filter.Matches(Row("SettingsChannel", "NotificationMessage", "ShowMessage", false, string.Empty)));
	}

	[TestMethod]
	public void ErrorTrueOnly()
	{
		var filter = BusHistoryFilter.Parse("error:true");
		IsTrue(filter.Matches(Row("Any", "X", "X", true, "boom")));
		IsFalse(filter.Matches(Row("Any", "X", "X", false, string.Empty)));
	}

	[TestMethod]
	public void FreeTextMatchesNameOrError()
	{
		var filter = BusHistoryFilter.Parse("ShowMessage");
		IsTrue(filter.Matches(Row("NotificationChannel", "X", "ShowMessage", false, string.Empty)));
		IsFalse(filter.Matches(Row("NotificationChannel", "X", "Other", false, string.Empty)));
		IsTrue(filter.Matches(Row("Any", "X", "Other", true, "ShowMessage failed")));
	}

	[TestMethod]
	public void TypeColonWithoutValueIsFreeText()
	{
		var filter = BusHistoryFilter.Parse("type:");
		IsFalse(filter.IsMatchAll);
	}

	[TestMethod]
	public void MatchesPublishResult()
	{
		var filter = BusHistoryFilter.Parse("channel:Test type:Named");
		var ok = new ChannelMessagePublishResult("TestChannel", "NamedPayload", null, 10, 1, false, string.Empty);
		var bad = new ChannelMessagePublishResult("Other", "NamedPayload", null, 10, 1, false, string.Empty);
		IsTrue(filter.Matches(ok));
		IsFalse(filter.Matches(bad));
	}

	private static ChannelMessageHistory Row(string channel, string type, string name, bool hadError, string error)
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
