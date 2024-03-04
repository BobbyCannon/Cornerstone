#region References

using Cornerstone.Protocols.Nmea;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Nmea.Messages;

public abstract class BaseMessageTests : CornerstoneUnitTest
{
	#region Methods

	protected void ProcessParseScenarios<T>((string sentance, T expected)[] scenarios)
		where T : NmeaMessage, new()
	{
		foreach (var scenario in scenarios)
		{
			scenario.expected.UpdateChecksum();
			scenario.expected.ToString().Dump();

			var actual = new T();
			actual.Parse(scenario.sentance);
			AreEqual(scenario.expected, actual);

			scenario.expected.UpdateChecksum();
			Assert.AreEqual(scenario.expected.ToString(), actual.ToString());
		}
	}

	#endregion
}