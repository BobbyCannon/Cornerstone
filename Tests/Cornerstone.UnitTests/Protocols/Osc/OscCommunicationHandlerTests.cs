#region References

using System;
using Cornerstone.Protocols.Osc;
using Cornerstone.UnitTests.Protocols.Samples;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Osc;

[TestClass]
public class OscCommunicationHandlerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void HandlerShouldBeCalled()
	{
		var actual = false;
		Func<object, SampleOscCommand, bool> test = (o, t) =>
		{
			actual = true;
			return actual;
		};

		var expected = new SampleOscCommand();
		var handler = new OscCommandHandler<SampleOscCommand>(test);
		handler.Process(handler, expected.ToMessage());
		IsTrue(actual, "Handler was not called");
	}

	#endregion
}